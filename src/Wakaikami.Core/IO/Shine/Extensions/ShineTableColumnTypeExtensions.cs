using Wakaikami.Core.IO.Shine.Enums;

namespace Wakaikami.Core.IO.Shine.Extensions;

public static class ShineTableColumnTypeExtensions
{
    public static Type ToClrType(this ShineTableColumnType columnType) =>
        columnType switch
        {
            ShineTableColumnType.Byte => typeof(byte),
            ShineTableColumnType.Word => typeof(short),
            ShineTableColumnType.DWord => typeof(uint),
            ShineTableColumnType.QWord => typeof(ulong),
            _ => typeof(string),
        };

    public static int GetFixedSize(this ShineTableColumnType columnType) =>
        columnType switch
        {
            ShineTableColumnType.Byte => 1,
            ShineTableColumnType.Word => 2,
            ShineTableColumnType.DWord => 4,
            ShineTableColumnType.QWord => 8,
            _ => -1,
        };

    public static ShineTableColumnType FromTypeName(string typeName)
    {
        var normalized = typeName.ToLowerInvariant().Trim();
        if (normalized.StartsWith("string[", StringComparison.Ordinal))
            return ShineTableColumnType.StringFixed;

        return normalized switch
        {
            "byte" => ShineTableColumnType.Byte,
            "word" => ShineTableColumnType.Word,
            "dwrd" or "dword" or "<integer>" => ShineTableColumnType.DWord,
            "qword" => ShineTableColumnType.QWord,
            "index" => ShineTableColumnType.Index,
            "string" or "<string>" => ShineTableColumnType.String,
            _ => ShineTableColumnType.String,
        };
    }

    public static int ParseStringLength(string typeName)
    {
        var normalized = typeName.ToLowerInvariant().Trim();
        if (!normalized.StartsWith("string[", StringComparison.Ordinal))
            return -1;

        var open = normalized.IndexOf('[');
        var close = normalized.IndexOf(']');
        if (open < 0 || close < 0 || close <= open)
            return -1;

        return int.TryParse(normalized.AsSpan(open + 1, close - open - 1), System.Globalization.CultureInfo.InvariantCulture, out var length) ? length : -1;
    }
}
