using System.Text;
using Wakaikami.Core.IO.Shine.Enums;

namespace Wakaikami.Core.IO.Shine;

internal sealed class ShineTableReaderCore
{
    private const string DirectiveTable = "#table";
    private const string DirectiveColumnType = "#columntype";
    private const string DirectiveColumnName = "#columnname";
    private const string DirectiveRecord = "#record";
    private const string DirectiveRecordIn = "#recordin";
    private const string DirectiveEnd = "#end";
    private const string DirectiveIgnore = "#ignore";
    private const string DirectiveExchange = "#exchange";
    private const string DirectiveDelimiter = "#delimiter";
    private const string DirectiveDefine = "#define";
    private const string DirectiveEndDefine = "#enddefine";

    private readonly Dictionary<string, TableBuilder> _tables = new(StringComparer.OrdinalIgnoreCase);
    private readonly List<string> _preTableLines = [];
    private readonly List<(string From, string To)> _exchanges = [];
    private readonly HashSet<char> _ignoreChars = [];

    private bool _foundFirstTable;
    private bool _inDefineBlock;
    private TableBuilder? _current;
    private List<string> _pendingTypes = [];
    private string _lastComment = string.Empty;

    public static ShineTableFile Load(string filename)
    {
        if (!File.Exists(filename))
            throw new FileNotFoundException($"Can't find Shine table file. {filename}", filename);

        var core = new ShineTableReaderCore();
        foreach (var line in File.ReadLines(filename, Encoding.Default))
            core.ProcessLine(line);

        return core.Build();
    }

    private ShineTableFile Build()
    {
        var tables = new Dictionary<string, ShineTable>(_tables.Count, StringComparer.OrdinalIgnoreCase);
        foreach (var (name, builder) in _tables)
            tables[name] = builder.Build();

        return new ShineTableFile(tables, _preTableLines.ToArray());
    }

    private void ProcessLine(string rawLine)
    {
        var (content, comment) = SplitComment(rawLine);
        _lastComment = comment.Trim();

        if (string.IsNullOrWhiteSpace(content))
        {
            if (!_foundFirstTable)
                _preTableLines.Add(rawLine);
            return;
        }

        var parts = SplitLine(content.Trim());
        if (TryProcessDirective(parts))
            return;

        if (!_foundFirstTable)
        {
            _preTableLines.Add(rawLine);
            return;
        }

        ProcessDataLine(parts);
    }

    private bool TryProcessDirective(string[] parts)
    {
        if (parts.Length == 0)
            return false;

        var directiveIndex = FindDirectiveIndex(parts);
        if (directiveIndex < 0)
            return false;

        var (directive, inlineArgument) = SplitDirectiveCell(parts[directiveIndex]);
        switch (directive)
        {
            case DirectiveIgnore:
            case DirectiveExchange:
            case DirectiveDelimiter:
                ProcessConfigDirective(directive, parts, directiveIndex);
                return true;

            case DirectiveDefine:
                _foundFirstTable = true;
                _current = GetOrCreateTable(GetTableName(parts, directiveIndex, inlineArgument));
                _inDefineBlock = true;
                return true;

            case DirectiveEndDefine:
                _inDefineBlock = false;
                return true;

            case DirectiveTable:
                _foundFirstTable = true;
                _current = GetOrCreateTable(GetTableName(parts, directiveIndex, inlineArgument));
                _pendingTypes.Clear();
                return true;

            case DirectiveColumnType:
                if (_current != null)
                    _pendingTypes = ParseNonEmptyTokens(parts, directiveIndex + 1);
                return true;

            case DirectiveColumnName:
                if (_current != null)
                    ProcessColumnNames(parts, directiveIndex);
                return true;

            case DirectiveRecordIn:
                ProcessRecordIn(parts, directiveIndex);
                return true;

            case DirectiveRecord:
                ProcessRecord(parts, directiveIndex);
                return true;

            case DirectiveEnd:
                _current = null;
                return true;

            default:
                return false;
        }
    }

    private void ProcessConfigDirective(string directive, string[] parts, int directiveIndex)
    {
        switch (directive)
        {
            case DirectiveIgnore:
                for (var i = directiveIndex + 1; i < parts.Length; i++)
                {
                    foreach (var ch in DecodeToken(parts[i].Trim()))
                        _ignoreChars.Add(ch);
                }
                break;

            case DirectiveExchange:
                if (directiveIndex + 2 < parts.Length)
                {
                    var from = DecodeToken(parts[directiveIndex + 1].Trim());
                    var to = DecodeToken(parts[directiveIndex + 2].Trim());
                    if (from.Length > 0)
                        _exchanges.Add((from, to));
                }
                break;

            case DirectiveDelimiter:
                // Custom record delimiters are not used by the shipped tables.
                break;
        }
    }

    private string GetTableName(string[] parts, int directiveIndex, string? inlineArgument)
    {
        if (!string.IsNullOrEmpty(inlineArgument))
            return inlineArgument;

        return directiveIndex + 1 >= parts.Length ? $"Table_{_tables.Count}" : parts[directiveIndex + 1].Trim();
    }

    private void ProcessColumnNames(string[] parts, int directiveIndex)
    {
        if (_current == null)
            return;

        var names = ParseNonEmptyTokens(parts, directiveIndex + 1);
        for (var i = 0; i < names.Count; i++)
        {
            var name = names[i];
            if (_current.HasColumn(name))
                continue;

            var rawTypeName = i < _pendingTypes.Count ? _pendingTypes[i] : "string";
            _current.AddColumn(ShineTableColumn.Create(name, rawTypeName));
        }
    }

    private void ProcessRecordIn(string[] parts, int directiveIndex)
    {
        if (directiveIndex + 1 >= parts.Length || !_tables.TryGetValue(parts[directiveIndex + 1].Trim(), out var builder))
            return;

        builder.AddRow(BuildRowValues(parts, directiveIndex + 2, builder.Columns));
    }

    private void ProcessRecord(string[] parts, int directiveIndex) => _current?.AddRow(BuildRowValues(parts, directiveIndex + 1, _current.Columns));

    private void ProcessDataLine(string[] parts)
    {
        if (_inDefineBlock && _current != null)
        {
            if (string.IsNullOrWhiteSpace(_lastComment))
                return;

            var rawTypeName = parts[0].Trim().ToLowerInvariant();
            _current.AddColumn(ShineTableColumn.Create(_lastComment, rawTypeName));
            return;
        }

        if (parts.Length < 2 || !_tables.TryGetValue(parts[0].Trim(), out var builder))
            return;

        builder.AddRow(BuildRowValues(parts, 1, builder.Columns));
    }

    private TableBuilder GetOrCreateTable(string name)
    {
        if (_tables.TryGetValue(name, out var existing))
            return existing;

        var builder = new TableBuilder(name);
        _tables.Add(name, builder);
        return builder;
    }

    private static int FindDirectiveIndex(string[] parts)
    {
        for (var i = 0; i < parts.Length; i++)
        {
            if (parts[i].Trim().StartsWith('#'))
                return i;
        }
        return -1;
    }

    private static (string Directive, string? InlineArgument) SplitDirectiveCell(string cell)
    {
        var trimmed = cell.Trim();
        var space = trimmed.IndexOf(' ');
        if (space < 0)
            return (trimmed.ToLowerInvariant(), null);

        return (trimmed[..space].ToLowerInvariant(), trimmed[(space + 1)..].Trim());
    }

    private static string[] SplitLine(string line) => line.Split('\t', StringSplitOptions.RemoveEmptyEntries);

    private static (string Content, string Comment) SplitComment(string line)
    {
        var index = line.IndexOf(';');
        return index < 0 ? (line, string.Empty) : (line[..index], line[(index + 1)..]);
    }

    private static List<string> ParseNonEmptyTokens(string[] parts, int startIndex)
    {
        var tokens = new List<string>(Math.Max(0, parts.Length - startIndex));
        for (var i = startIndex; i < parts.Length; i++)
        {
            var token = parts[i].Trim();
            if (token.Length > 0)
                tokens.Add(token);
        }
        return tokens;
    }

    private object[] BuildRowValues(string[] parts, int startIndex, IReadOnlyList<ShineTableColumn> columns)
    {
        var count = columns.Count;
        if (count == 0)
            return [];

        var values = new object[count];
        var c = 0;
        for (var i = startIndex; i < parts.Length && c < count; i++, c++)
        {
            var token = parts[i].Trim();
            if (token.Length > 0)
                token = token.TrimEnd(',').Trim('"');
            token = ApplyTransforms(token);
            values[c] = ConvertValue(columns[c].Type, token);
        }

        for (; c < count; c++)
            values[c] = DefaultValue(columns[c].Type);

        return values;
    }

    private string ApplyTransforms(string token)
    {
        if (token.Length == 0)
            return token;

        foreach (var (from, to) in _exchanges)
            token = token.Replace(from, to);

        if (_ignoreChars.Count > 0 && ContainsIgnored(token))
        {
            var sb = new StringBuilder(token.Length);

            foreach (var ch in token.Where(ch => !_ignoreChars.Contains(ch)))
            {
                sb.Append(ch);
            }
            token = sb.ToString();
        }

        return token;
    }

    private bool ContainsIgnored(string token)
    {
        return token.Any(ch => _ignoreChars.Contains(ch));
    }

    private static string DecodeToken(string token)
    {
        if (token.Length < 2 || token[0] != '\\')
            return token;

        var marker = char.ToLowerInvariant(token[1]);
        var rest = token[2..];
        try
        {
            return marker switch
            {
                'x' => ((char)Convert.ToInt32(rest, 16)).ToString(),
                'o' => ((char)Convert.ToInt32(rest, 8)).ToString(),
                'd' => ((char)int.Parse(rest, System.Globalization.CultureInfo.InvariantCulture)).ToString(),
                't' => "\t",
                'n' => "\n",
                _ => token,
            };
        }
        catch (Exception ex) when (ex is FormatException or OverflowException or ArgumentException)
        {
            return token;
        }
    }

    private static object ConvertValue(ShineTableColumnType type, string token) =>
        type switch
        {
            ShineTableColumnType.Byte => byte.TryParse(token, out var b) ? b : (byte)0,
            ShineTableColumnType.Word => short.TryParse(token, System.Globalization.CultureInfo.InvariantCulture, out var w) ? w : (short)0,
            ShineTableColumnType.DWord => uint.TryParse(token, out var d) ? d : 0u,
            ShineTableColumnType.QWord => ulong.TryParse(token, out var q) ? q : 0UL,
            _ => token,
        };

    private static object DefaultValue(ShineTableColumnType type) =>
        type switch
        {
            ShineTableColumnType.Byte => (byte)0,
            ShineTableColumnType.Word => (short)0,
            ShineTableColumnType.DWord => 0u,
            ShineTableColumnType.QWord => 0UL,
            _ => string.Empty,
        };

    private sealed class TableBuilder(string name)
    {
        private readonly List<ShineTableColumn> _columns = [];
        private readonly HashSet<string> _columnNames = new(StringComparer.OrdinalIgnoreCase);
        private readonly List<object[]> _rows = [];

        public IReadOnlyList<ShineTableColumn> Columns => _columns;

        public bool HasColumn(string columnName) => _columnNames.Contains(columnName);

        public void AddColumn(ShineTableColumn column)
        {
            _columns.Add(column);
            _columnNames.Add(column.Name);
        }

        public void AddRow(object[] values) => _rows.Add(values);

        public ShineTable Build() => new(name, _columns.ToArray(), _rows.ToArray());
    }
}
