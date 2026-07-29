using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using Wakaikami.WorldService.Content.Entity.Character.Interfaces;

namespace Wakaikami.WorldService.Content.Entity.Character;

/// <summary>Characters currently live on this world, keyed by character id. Character-select loads
/// detached instances into <see cref="WorldCharacterList"/>; only the one the account actually enters
/// the world with is registered here.</summary>
public sealed class CharacterManager : ICharacterManager
{
    private readonly ConcurrentDictionary<int, WorldCharacter> _charactersById = new();

    public bool GetCharacterById(int id, [NotNullWhen(true)] out WorldCharacter? character) => _charactersById.TryGetValue(id, out character);

    public bool Add(WorldCharacter character) => _charactersById.TryAdd(character.Id, character);

    public bool Remove(int id) => _charactersById.TryRemove(id, out _);

    /// <summary>Consistent snapshot of the live characters - read the length and iterate off the same
    /// array instead of pairing a count with a separate enumeration.</summary>
    public WorldCharacter[] ToArray() => [.. _charactersById.Values];
}
