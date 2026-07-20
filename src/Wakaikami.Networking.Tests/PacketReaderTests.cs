using System.Text;
using Wakaikami.Networking.Protocol.Abstractions;
using Xunit;

namespace Wakaikami.Networking.Tests;

[Collection("PacketStream")]
public sealed class PacketReaderTests : IDisposable
{
    private static readonly Encoding _testEncoding = Encoding.UTF8;

    public void Dispose() { }

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
    public void ReadInt32RoundTripReturnsSameValue()
    {
        const int source = -42;
        var data = BitConverter.GetBytes(source);
        var reader = new PacketReader(data);

        var ok = reader.Read(out int result);

        Assert.True(ok);
        Assert.Equal(source, result);
    }

    [Fact]
    public void ReadByteRoundTripReturnsSameValue()
    {
        var data = new byte[] { 0xFF };
        var reader = new PacketReader(data);

        var ok = reader.Read(out byte result);

        Assert.True(ok);
        Assert.Equal((byte)0xFF, result);
    }

    [Fact]
    public void ReadMultipleFieldsReadsInOrder()
    {
        var stream = new MemoryStream();
        using var bw = new BinaryWriter(stream, _testEncoding);
        bw.Write((byte)1);
        bw.Write((ushort)2);
        bw.Write((uint)3);
        var data = stream.ToArray();

        var reader = new PacketReader(data);

        Assert.True(reader.Read(out byte b));
        Assert.True(reader.Read(out ushort s));
        Assert.True(reader.Read(out uint i));
        Assert.Equal((byte)1, b);
        Assert.Equal((ushort)2, s);
        Assert.Equal((uint)3, i);
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
    public void ReadBytesRoundTripReturnsSameData()
    {
        var source = new byte[] { 1, 2, 3, 4, 5 };
        var reader = new PacketReader(source);

        Assert.True(reader.ReadBytes(source.Length, out var result));
        Assert.Equal(source, result);
    }

    [Fact]
    public void ReadBytesNotEnoughDataReturnsFalse()
    {
        var source = new byte[] { 1, 2 };
        var reader = new PacketReader(source);

        Assert.False(reader.ReadBytes(10, out _));
    }

    [Fact]
    public void ReadStringLengthPrefixedReadsCorrectly()
    {
        const string text = "Hello";
        var textBytes = _testEncoding.GetBytes(text);
        var data = new byte[1 + textBytes.Length];
        data[0] = (byte)textBytes.Length;
        Buffer.BlockCopy(textBytes, 0, data, 1, textBytes.Length);
        var reader = new PacketReader(data);

        var ok = reader.ReadString(out var result);

        Assert.True(ok);
        Assert.Equal(text, result);
    }
}
