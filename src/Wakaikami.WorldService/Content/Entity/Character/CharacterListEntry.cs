using System.Numerics;
using Wakaikami.Content.Entity.Character.Data;

namespace Wakaikami.WorldService.Content.Entity.Character;

public sealed class CharacterListEntry
{
    public int Id { get; }
    public byte Slot { get; }
    public string Name { get; }
    public ushort Level { get; }
    public string MapIndex { get; }
    public ushort InstanceId { get; }
    public Vector2 Position { get; }
    public CharacterShapeData Shape { get; }

    public CharacterListEntry(WorldCharacter character)
    {
        Id = character.Id;
        Slot = character.Slot;
        Name = character.Name;
        Level = character.Level;
        MapIndex = character.MapIndex;
        InstanceId = character.InstanceId;
        Position = character.Position;
        Shape = character.Shape.ToData();
    }

    private CharacterListEntry(CharacterListEntry source, string name)
    {
        Id = source.Id;
        Slot = source.Slot;
        Name = name;
        Level = source.Level;
        MapIndex = source.MapIndex;
        InstanceId = source.InstanceId;
        Position = source.Position;
        Shape = source.Shape;
    }

    /// <summary>Returns a copy carrying <paramref name="name"/>, for refreshing the roster after a rename.</summary>
    public CharacterListEntry WithName(string name) => new(this, name);
}
