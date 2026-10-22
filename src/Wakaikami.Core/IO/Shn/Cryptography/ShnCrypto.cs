namespace Wakaikami.Core.IO.Shn.Cryptography;

public static class ShnCrypto
{
    public static void Crypt(Span<byte> data)
    {
        var num = (byte)data.Length;
        for (var i = data.Length - 1; i >= 0; i--)
        {
            data[i] ^= num;
            var next = (byte)(i & 15);
            next = (byte)(next + 0x55);
            next = (byte)(next ^ (byte)(((byte)i) * 11));
            next = (byte)(next ^ num);
            next = (byte)(next ^ 170);
            num = next;
        }
    }
}
