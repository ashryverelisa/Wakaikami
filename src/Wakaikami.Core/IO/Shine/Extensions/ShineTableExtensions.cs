namespace Wakaikami.Core.IO.Shine.Extensions;

public static class ShineTableExtensions
{
    public static IEnumerable<ShineTableRow> EnumerateRows(this ShineTable table)
    {
        var index = new Dictionary<string, int>(table.Columns.Count, StringComparer.OrdinalIgnoreCase);
        for (var i = 0; i < table.Columns.Count; i++)
            index[table.Columns[i].Name] = i;

        foreach (var row in table.Rows)
            yield return new ShineTableRow(row, index);
    }
}
