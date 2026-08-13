namespace Wakaikami.Core.IO.Shine;

public sealed class ShineTable
{
    public string Name { get; }
    public IReadOnlyList<ShineTableColumn> Columns { get; }
    public IReadOnlyList<object[]> Rows { get; }

    internal ShineTable(string name, ShineTableColumn[] columns, object[][] rows)
    {
        Name = name;
        Columns = columns;
        Rows = rows;
    }
}
