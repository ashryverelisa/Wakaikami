using System.Collections.Concurrent;

namespace Wakaikami.WorldService.Content.Entity.Character;

public sealed class WorldCharacterList
{
    public int Count => _bySlot.Count;

    public ushort HighestLevel
    {
        get
        {
            ushort level = 1;
            foreach (var entry in _bySlot.Values)
                if (entry.Level > level)
                    level = entry.Level;

            return level;
        }
    }

    private readonly ConcurrentDictionary<byte, CharacterListEntry> _bySlot = new();

    public bool Add(CharacterListEntry entry) => _bySlot.TryAdd(entry.Slot, entry);

    public bool Remove(byte slot) => _bySlot.TryRemove(slot, out _);

    public bool Contains(byte slot) => _bySlot.ContainsKey(slot);

    public CharacterListEntry? this[byte slot] => _bySlot.GetValueOrDefault(slot);

    public bool Rename(byte slot, string newName)
    {
        if (!_bySlot.TryGetValue(slot, out var entry))
            return false;

        return _bySlot.TryUpdate(slot, entry.WithName(newName), entry);
    }

    public void Clear() => _bySlot.Clear();

    /// <summary>Consistent snapshot of the current entries - read the length and iterate off the same
    /// array instead of pairing <see cref="Count"/> with a separate enumeration (mirrors
    /// <c>CharacterManager.ToArray</c>). Bounded by <c>WorldConstants.MaxCharacterPerWorld</c>.</summary>
    public CharacterListEntry[] ToArray() => [.. _bySlot.Values];
}
