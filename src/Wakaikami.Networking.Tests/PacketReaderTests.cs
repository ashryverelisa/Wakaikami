using System.Text;
using Wakaikami.Networking.Protocol.Abstractions;
using Xunit;

namespace Wakaikami.Networking.Tests;

public sealed class PacketReaderTests
{
    private static readonly Encoding _testEncoding = Encoding.UTF8;

    /// <summary>
    /// One representative width for the generic <c>Read&lt;T&gt;</c>: the value itself comes straight from
    /// MemoryMarshal, what this pins is that the read advances the cursor by exactly the field size.
    /// </summary>
    [Fact]
    public void ReadUInt16RoundTripReturnsSameValue()
    {
        const ushort source = 42;
        var data = BitConverter.GetBytes(source);
        var reader = new PacketReader(data);

        var ok = reader.Read(out ushort result);

        Assert.True(ok);
        Assert.Equal(source, result);
        Assert.Equal(0, reader.Remaining);
    }

    [Fact]
    public void ReadEmptyBufferReturnsFalse()
    {
        var reader = new PacketReader([]);

        var ok = reader.Read(out byte _);

        Assert.False(ok);
    }

    [Fact]
    public void ReadNotEnoughDataReturnsFalse()
    {
        var data = new byte[] { 0x01 };
        var reader = new PacketReader(data);

        var ok = reader.Read(out ushort _);

        Assert.False(ok);
    }

    [Fact]
    public void ReadStringValidLengthReadsCorrectly()
    {
        var stream = new MemoryStream();
        using var bw = new BinaryWriter(stream, _testEncoding);
        bw.Write((byte)5); // length prefix
        bw.Write(_testEncoding.GetBytes("Hello"));
        var data = stream.ToArray();

        var reader = new PacketReader(data);

        Assert.True(reader.ReadString(out var result));
        Assert.Equal("Hello", result);
    }

    [Fact]
    public void ReadStringWithExplicitLengthReadsCorrectly()
    {
        var data = _testEncoding.GetBytes("World");
        var reader = new PacketReader(data);

        Assert.True(reader.ReadString(out var result, 5));
        Assert.Equal("World", result);
    }

    [Fact]
    public void ReadGuidFromHexStringReturnsSameValue()
    {
        var source = Guid.NewGuid();
        var hex = source.ToString("N"); // 32 hex chars
        var data = _testEncoding.GetBytes(hex);
        var reader = new PacketReader(data);

        Assert.True(reader.ReadGuid(out var result));
        Assert.Equal(source, result);
    }

    [Fact]
    public void ReadBytesNotEnoughDataReturnsFalse()
    {
        var source = new byte[] { 1, 2 };
        var reader = new PacketReader(source);

        Assert.False(reader.ReadBytes(10, out _));
    }
}
