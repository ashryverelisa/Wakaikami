using Wakaikami.Networking.Protocol.Abstractions;
using Xunit;

namespace Wakaikami.Networking.Tests;

[Collection("PacketStream")]
public sealed class PacketWriterTests : IDisposable
{
    public void Dispose() { }

    [Fact]
    public void WriteThenReadUInt16RoundTrip()
    {
        using var writer = new PacketWriter();
        writer.Write((ushort)42);
        var reader = new PacketReader(writer.ToArray());

        Assert.True(reader.Read(out ushort result));
        Assert.Equal((ushort)42, result);
    }

    [Fact]
    public void WriteThenReadMultipleValuesRoundTrip()
    {
        using var writer = new PacketWriter();
        writer.Write((byte)1);
        writer.Write((ushort)2);
        writer.Write((uint)3);
        writer.WriteString("Hello", 5);

        var reader = new PacketReader(writer.ToArray());

        Assert.True(reader.Read(out byte b));
        Assert.True(reader.Read(out ushort s));
        Assert.True(reader.Read(out uint i));
        Assert.True(reader.ReadString(out var str, 5));

        Assert.Equal((byte)1, b);
        Assert.Equal((ushort)2, s);
        Assert.Equal((uint)3, i);
        Assert.Equal("Hello", str);
    }

    [Fact]
    public void WriteStringValueLongerThanFieldTruncatesInsteadOfThrowing()
    {
        using var writer = new PacketWriter();

        writer.WriteString("ABCDEFGH", 4); // 8 chars into a 4-byte field

        var reader = new PacketReader(writer.ToArray());
        Assert.True(reader.ReadString(out var result, 4));
        Assert.Equal("ABCD", result);
    }

    [Fact]
    public void WriteStringMultiByteCharAtBoundaryTruncatesAtCharBoundary()
    {
        using var writer = new PacketWriter();

        writer.WriteString("ABCä", 4); // 'ä' needs 2 UTF-8 bytes; only 1 remains -> dropped, zero-padded

        var reader = new PacketReader(writer.ToArray());
        Assert.True(reader.ReadString(out var result, 4));
        Assert.Equal("ABC", result);
    }

    [Fact]
    public void WriteThenReadStringLengthPrefixedRoundTrip()
    {
        const string text = "PacketTest";
        using var writer = new PacketWriter();
        writer.Write((byte)text.Length);
        writer.WriteString(text, text.Length);

        var reader = new PacketReader(writer.ToArray());

        Assert.True(reader.ReadString(out var result));
        Assert.Equal(text, result);
    }

    [Fact]
    public void WriteThenReadFloatRoundTrip()
    {
        using var writer = new PacketWriter();
        writer.Write(3.14f);

        var reader = new PacketReader(writer.ToArray());

        Assert.True(reader.Read(out float result));
        Assert.Equal(3.14f, result);
    }

    [Fact]
    public void WriteThenReadGuidAsHexRoundTrip()
    {
        var guid = Guid.NewGuid();
        var hex = guid.ToString("N");
        using var writer = new PacketWriter();
        writer.WriteString(hex, 32);

        var reader = new PacketReader(writer.ToArray());

        Assert.True(reader.ReadGuid(out var result));
        Assert.Equal(guid, result);
    }

    [Fact]
    public void WriteThenReadBytesRoundTrip()
    {
        var source = new byte[] { 10, 20, 30, 40 };
        using var writer = new PacketWriter();
        writer.Write(source);

        var reader = new PacketReader(writer.ToArray());

        Assert.True(reader.ReadBytes(source.Length, out var result));
        Assert.Equal(source, result);
    }

    [Fact]
    public void WriteThenReadInt32NegativeRoundTrip()
    {
        using var writer = new PacketWriter();
        writer.Write(-12345);

        var reader = new PacketReader(writer.ToArray());

        Assert.True(reader.Read(out int result));
        Assert.Equal(-12345, result);
    }
}
