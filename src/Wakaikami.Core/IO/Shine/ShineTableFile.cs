namespace Wakaikami.Core.IO.Shine;

public sealed class ShineTableFile
{
    private readonly Dictionary<string, ShineTable> _tables;

    internal ShineTableFile(Dictionary<string, ShineTable> tables, string[] preTableLines)
    {
        _tables = tables;
        PreTableLines = preTableLines;
    }

    public IReadOnlyList<string> PreTableLines { get; }

    public IReadOnlyDictionary<string, ShineTable> Tables => _tables;

    public ShineTable? this[string name] => _tables.GetValueOrDefault(name);
}