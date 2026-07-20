using System.Buffers;

namespace Wakaikami.Networking.Protocol.Abstractions.Interfaces;

public interface IPacketParser
{
    public ValueTask<ParseResult> ParseAsync(ReadOnlySequence<byte> buffer, CancellationToken cancellationToken);
}
