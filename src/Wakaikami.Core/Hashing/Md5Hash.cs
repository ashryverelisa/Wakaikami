using System.Security.Cryptography;

namespace Wakaikami.Core.Hashing;

public static class Md5Hash
{
    public static string CalculateMd5Hash(byte[] buffer)
    {
        var hash = MD5.HashData(buffer);
        return Convert.ToHexStringLower(hash);
    }
}
