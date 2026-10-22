using Wakaikami.Core.IO.Shn.Enums;

namespace Wakaikami.Core.IO.Shn;

public sealed record ShnColumn(string Name, ShnColumnType ShnType, int ShnLength, Type DataType);
