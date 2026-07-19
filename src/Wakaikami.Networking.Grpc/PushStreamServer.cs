using Grpc.Core;

namespace Wakaikami.Networking.Grpc;

public static class PushStreamServer
{
    public static async Task PumpAsync<TPush>(
        IAsyncEnumerable<TPush> pushes,
        IServerStreamWriter<TPush> responseStream,
        TPush initialPush,
        Action onStreamEnded,
        CancellationToken cancellationToken
    )
    {
        try
        {
            await responseStream.WriteAsync(initialPush, cancellationToken);

            await foreach (var push in pushes)
                await responseStream.WriteAsync(push, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Client went away / reconnecting - normal stream teardown, not an error.
        }
        finally
        {
            onStreamEnded();
        }
    }
}
