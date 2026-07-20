using Wakaikami.Networking.Protocol.Abstractions;

namespace Wakaikami.Networking.Protocol.Fiesta;

public abstract class FiestaClientPacket(byte[] buffer) : FiestaPacket
{
    protected PacketReader Reader { get; } = new(buffer);

    public abstract bool Read();

    /// <summary>Returns diagnostic info when <see cref="Read"/> returns <c>false</c>.</summary>
    public (long Position, long Remaining, string? Field) GetReadError() => (Reader.LastReadPosition, Reader.Remaining, Reader.LastFailedField);

    protected override byte[] BodyBytes() => Reader.ToArray();
}
