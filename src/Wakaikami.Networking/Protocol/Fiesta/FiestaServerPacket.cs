using System.Buffers.Binary;
using Wakaikami.Networking.HandlerTypes;
using Wakaikami.Networking.Protocol.Abstractions.Interfaces;

namespace Wakaikami.Networking.Protocol.Fiesta;

public abstract class FiestaServerPacket(FiestaHandlerType header, ushort type) : FiestaPacket(header, type), IServerPacket
{
    public FiestaPacketWriter Writer { get; } = new();

    public abstract void Write();

    protected override byte[] BodyBytes() => Writer.ToArray();

    /// <summary>Full framed packet (size-prefix + opcode + body). Tests/broadcast staging; the hot send path uses WireSize + WriteTo.</summary>
    public byte[] ToArray()
    {
        var result = new byte[WireSize];
        WriteTo(result);
        return result;
    }

    public int WireSize
    {
        get
        {
            var bodyLen = Writer.Length;
            var sizeHeader = (bodyLen <= byte.MaxValue) ? 1 : 3;
            return sizeHeader + 2 + bodyLen;
        }
    }

    public int WriteTo(Span<byte> destination)
    {
        var bodyLen = Writer.Length;
        var totalSize = (ushort)(bodyLen + 2);
        var sizeHeader = (bodyLen <= byte.MaxValue) ? 1 : 3;
        var total = sizeHeader + 2 + bodyLen;

        if (destination.Length < total)
            throw new ArgumentException("Destination span too small for packet.", nameof(destination));

        int offset;
        if (sizeHeader == 1)
        {
            destination[0] = (byte)totalSize;
            offset = 1;
        }
        else
        {
            destination[0] = 0;
            BinaryPrimitives.WriteUInt16LittleEndian(destination[1..], totalSize);
            offset = 3;
        }

        BinaryPrimitives.WriteUInt16LittleEndian(destination[offset..], GetOpcode());
        offset += 2;

        Writer.WrittenSpan.CopyTo(destination[offset..]);

        return total;
    }

    protected override void DisposeInternal() => Writer.Dispose();
}
