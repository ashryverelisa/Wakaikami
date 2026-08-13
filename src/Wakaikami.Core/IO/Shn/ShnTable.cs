namespace Wakaikami.Core.IO.Shn;

public sealed class ShnTable
{
    public IReadOnlyList<ShnColumn> Columns { get; }
    public IReadOnlyList<object[]> Rows { get; }
    public string Md5Hash { get; }

    internal ShnTable(ShnColumn[] columns, object[][] rows, string md5Hash)
    {
        Columns = columns;
        Rows = rows;
        Md5Hash = md5Hash;
    }
}