namespace Wakaikami.Core.IO.Shn;

public static class ShnTableExtensions
{
    public static IEnumerable<ShnRow> EnumerateRows(this ShnTable table)
    {
        var index = new Dictionary<string, int>(table.Columns.Count, StringComparer.OrdinalIgnoreCase);
        for (var i = 0; i < table.Columns.Count; i++)
            index[table.Columns[i].Name] = i;

        foreach (var row in table.Rows)
            yield return new ShnRow(row, index);
    }
}