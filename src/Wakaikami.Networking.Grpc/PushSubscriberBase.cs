using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Wakaikami.Networking.Grpc;

public abstract class PushSubscriberBase<TPush>(ILogger logger) : BackgroundService
{
    private static readonly TimeSpan _reconnectDelay = TimeSpan.FromSeconds(5);
    private static readonly TimeSpan _registerRetryDelay = TimeSpan.FromSeconds(3);

    protected ILogger Logger { get; } = logger;

    protected abstract string StreamDescription { get; }

    protected abstract IAsyncEnumerable<TPush> SubscribeAsync(CancellationToken cancellationToken);

    protected abstract Task DispatchAsync(TPush push, CancellationToken cancellationToken);

    protected abstract void OnSubscriptionLive(CancellationToken connectionToken);

    protected virtual Task<bool> OnBeforeSubscribeAsync(CancellationToken connectionToken) => Task.FromResult(true);

    protected async Task<TReply?> RegisterUntilAcceptedAsync<TReply>(
        Func<CancellationToken, Task<TReply>> registerAsync,
        Func<TReply, bool> isAccepted,
        string operation,
        CancellationToken cancellationToken
    )
        where TReply : class
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                var reply = await registerAsync(cancellationToken);
                if (isAccepted(reply))
                {
                    Logger.GrpcRegisterAccepted(operation);
                    return reply;
                }

                Logger.GrpcRegisterPending(operation);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                return null;
            }
            catch (Exception ex)
            {
                Logger.GrpcRegisterAttemptFailed(ex, operation);
            }

            try
            {
                await Task.Delay(_registerRetryDelay, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                return null;
            }
        }

        return null;
    }

    protected async Task SyncLoopAsync(Func<CancellationToken, Task> syncAsync, TimeSpan interval, string operation, CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(interval, cancellationToken);
                await syncAsync(cancellationToken);
            }
            catch (OperationCanceledException)
            {
                return;
            }
            catch (Exception ex)
            {
                Logger.GrpcSyncFailed(ex, operation);
            }
        }
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            // Tie the connection lifecycle to the subscription lifetime: cancel everything we started for
            // this stream when it ends, and re-run it on the next (re)connect.
            var connectionCts = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
            try
            {
                if (!await OnBeforeSubscribeAsync(connectionCts.Token))
                    break;

                Logger.PushStreamSubscribing(StreamDescription);

                var live = false;
                await foreach (var push in SubscribeAsync(connectionCts.Token))
                {
                    if (!live)
                    {
                        live = true;
                        OnSubscriptionLive(connectionCts.Token);
                    }

                    await DispatchAsync(push, connectionCts.Token);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                Logger.PushStreamDropped(ex, StreamDescription, _reconnectDelay.TotalSeconds);
                try
                {
                    await Task.Delay(_reconnectDelay, stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
            finally
            {
                await connectionCts.CancelAsync();
                connectionCts.Dispose();
            }
        }
    }
}
