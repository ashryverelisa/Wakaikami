using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Threading.Channels;

namespace Wakaikami.Networking.Grpc;

public abstract class PushHub<TPush>
{
    private readonly ConcurrentDictionary<int, Channel<TPush>> _channels = new();

    public async IAsyncEnumerable<TPush> Subscribe(int id, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var channel = Channel.CreateUnbounded<TPush>(new UnboundedChannelOptions { SingleReader = true });

        if (_channels.TryGetValue(id, out var previous))
            previous.Writer.TryComplete();
        _channels[id] = channel;
        try
        {
            await foreach (var push in channel.Reader.ReadAllAsync(cancellationToken))
                yield return push;
        }
        finally
        {
            // Only remove if this exact channel is still the registered one (a newer subscribe wins).
            _channels.TryRemove(KeyValuePair.Create(id, channel));
        }
    }

    /// <summary>Sends to a single subscriber. Returns false if no subscriber is registered for <paramref name="id"/>.</summary>
    public bool Publish(int id, TPush push) => _channels.TryGetValue(id, out var channel) && channel.Writer.TryWrite(push);

    /// <summary>Sends to every subscriber. Returns whether at least one subscriber received it.</summary>
    public bool Broadcast(TPush push)
    {
        var any = false;
        foreach (var channel in _channels.Values)
            any |= channel.Writer.TryWrite(push);
        return any;
    }
}
