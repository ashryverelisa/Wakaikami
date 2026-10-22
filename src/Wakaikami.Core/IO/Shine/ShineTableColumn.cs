using Wakaikami.Core.IO.Shine.Enums;
using Wakaikami.Core.IO.Shine.Extensions;

namespace Wakaikami.Core.IO.Shine;

public sealed record ShineTableColumn(string Name, ShineTableColumnType Type, int Length, string RawTypeName, Type DataType)
{
    public static ShineTableColumn Create(string name, string rawTypeName)
    {
        var type = ShineTableColumnTypeExtensions.FromTypeName(rawTypeName);
        var length = type == ShineTableColumnType.StringFixed ? ShineTableColumnTypeExtensions.ParseStringLength(rawTypeName) : type.GetFixedSize();

        return new ShineTableColumn(name, type, length, rawTypeName, type.ToClrType());
    }
}
