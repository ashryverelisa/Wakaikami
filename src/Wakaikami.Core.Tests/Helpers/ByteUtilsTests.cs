using Wakaikami.Core.Helpers;

namespace Wakaikami.Core.Tests.Helpers;

public sealed class ByteUtilsTests
{
    [Fact]
    public void HexToBytesIgnoresSpaces()
    {
        Assert.Equal(new byte[] { 0xDE, 0xAD, 0xBE, 0xEF }, "DE AD BE EF".HexToBytes());
    }
}
