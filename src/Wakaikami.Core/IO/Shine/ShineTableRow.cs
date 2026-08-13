namespace Wakaikami.Core.IO.Shine;

public readonly struct ShineTableRow
{
    private readonly object[] _values;
    private readonly IReadOnlyDictionary<string, int> _index;

    internal ShineTableRow(object[] values, IReadOnlyDictionary<string, int> index)
    {
        _values = values;
        _index = index;
    }

    public T Get<T>(string column)
    {
        var value = _values[_index[column]];
        if (value is T typed)
            return typed;
        return (T)Convert.ChangeType(value, typeof(T), System.Globalization.CultureInfo.InvariantCulture);
    }
    
    public object Get(Type targetType, string column)
    {
        var value = _values[_index[column]];
        return targetType.IsInstanceOfType(value) ? value : Convert.ChangeType(value, targetType, System.Globalization.CultureInfo.InvariantCulture);
    }

    public bool TryGet<T>(string column, out T value)
    {
        if (!_index.TryGetValue(column, out var idx))
        {
            value = default!;
            return false;
        }

        var raw = _values[idx];
        if (raw is T typed)
        {
            value = typed;
            return true;
        }

        value = (T)Convert.ChangeType(raw, typeof(T), System.Globalization.CultureInfo.InvariantCulture);
        return true;
    }
}