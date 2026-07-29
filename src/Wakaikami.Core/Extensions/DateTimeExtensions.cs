namespace Wakaikami.Core.Extensions;

public static class DateTimeExtensions
{
    private const int NeverIfNullSentinel = 1992027391;

    public static int ToBitPackedInt32(this DateTime value, bool neverIfNull = false)
    {
        var packed = 0;

        packed |= value.Minute << 25;
        packed |= (value.Hour & 0x3F) << 19;
        packed |= (value.Day & 0x3F) << 13;
        packed |= (value.Month & 0x1F) << 8;
        packed |= (byte)(value.Year - 2000);

        return neverIfNull && packed == 0 ? NeverIfNullSentinel : packed;
    }
}
