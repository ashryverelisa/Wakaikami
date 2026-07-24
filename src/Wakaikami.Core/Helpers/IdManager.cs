namespace Wakaikami.Core.Helpers;

public class IdManager
{
    private readonly Queue<ushort> _availableIds;

    private readonly HashSet<ushort> _availableSet;
    private readonly Lock _lock = new();

    public IdManager(ushort minValue, ushort maxValue)
    {
        if (minValue > maxValue)
        {
            throw new ArgumentException("minValue must not be greater than maxValue.", nameof(minValue));
        }

        var capacity = maxValue - minValue + 1;
        _availableIds = new Queue<ushort>(capacity);
        _availableSet = new HashSet<ushort>(capacity);
        for (var i = minValue; i <= maxValue; i++)
        {
            _availableIds.Enqueue(i);
            _availableSet.Add(i);
        }
    }

    public bool TryGetNewId(out ushort id)
    {
        lock (_lock)
        {
            if (_availableIds.TryDequeue(out id))
            {
                _availableSet.Remove(id);
                return true;
            }

            id = 0;
            return false;
        }
    }

    public bool TryReleaseId(ushort id)
    {
        lock (_lock)
        {
            if (!_availableSet.Add(id))
                return false;
            _availableIds.Enqueue(id);
            return true;
        }
    }
}
