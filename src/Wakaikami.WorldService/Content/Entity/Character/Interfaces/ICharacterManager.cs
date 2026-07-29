using System.Diagnostics.CodeAnalysis;

namespace Wakaikami.WorldService.Content.Entity.Character.Interfaces;

public interface ICharacterManager
{
    public bool GetCharacterById(int id, [NotNullWhen(true)] out WorldCharacter? character);

    public bool Add(WorldCharacter character);

    public bool Remove(int id);

    public WorldCharacter[] ToArray();
}
