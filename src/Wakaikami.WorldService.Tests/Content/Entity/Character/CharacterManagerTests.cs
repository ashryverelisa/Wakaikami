using Wakaikami.WorldService.Content.Entity.Character;

namespace Wakaikami.WorldService.Tests.Content.Entity.Character;

public sealed class CharacterManagerTests
{
    private static WorldCharacter NewCharacter(int id, string name = "Hero") => new() { Id = id, Name = name };

    [Fact]
    public void AddDuplicateIdReturnsFalseAndKeepsTheFirst()
    {
        var manager = new CharacterManager();
        var first = NewCharacter(1);
        manager.Add(first);

        Assert.False(manager.Add(NewCharacter(1, "Impostor")));
        Assert.True(manager.GetCharacterById(1, out var found));
        Assert.Same(first, found);
    }

    [Fact]
    public void ToArrayIsASnapshotAndDoesNotChangeWithLaterAdds()
    {
        var manager = new CharacterManager();
        manager.Add(NewCharacter(1));

        var snapshot = manager.ToArray();
        manager.Add(NewCharacter(2));

        Assert.Single(snapshot);
    }
}
