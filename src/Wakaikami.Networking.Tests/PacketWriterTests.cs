using Wakaikami.Networking.Protocol.Abstractions;
using Xunit;

namespace Wakaikami.Networking.Tests;

/// <summary>
/// Fixed-width string fields are the only place the writer makes a decision of its own - everything else
/// is a straight copy into the buffer. Round-tripping writer against reader is deliberately not asserted
/// here: a symmetric mistake in both would pass.
/// </summary>
public sealed class PacketWriterTests
{
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
}
