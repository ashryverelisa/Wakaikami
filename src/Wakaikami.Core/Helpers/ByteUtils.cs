namespace Wakaikami.Core.Helpers;

public static class ByteUtils
{
    public static byte[] HexToBytes(this string hex) => Convert.FromHexString(hex.Replace(" ", ""));

    public static string BytesToHex(this byte[] bytes) => Convert.ToHexString(bytes);
}
