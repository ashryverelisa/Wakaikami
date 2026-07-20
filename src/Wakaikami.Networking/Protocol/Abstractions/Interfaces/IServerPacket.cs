namespace Wakaikami.Networking.Protocol.Abstractions.Interfaces;

public interface IServerPacket : IDisposable
{
    public void Write();
    public byte[] ToArray();

    /// <summary>Total bytes the framed packet occupies on the wire (size-prefix + opcode + body).</summary>
    public int WireSize { get; }

    /// <summary>Serializes the framed packet into <paramref name="destination"/>; returns the number of bytes written.</summary>
    public int WriteTo(Span<byte> destination);
}
