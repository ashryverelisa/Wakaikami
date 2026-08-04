using Wakaikami.WorldService.Content.Entity.Character;

namespace Wakaikami.WorldService.Tests.Content.Entity.Character;

public sealed class WorldCharacterListTests
{
    private static CharacterListEntry Entry(byte slot, int id = 1, string name = "Hero") =>
        new(
            new WorldCharacter
            {
                Id = id,
                Slot = slot,
                Name = name,
            }
        );

    [Fact]
    public void AddOccupiedSlotReturnsFalseAndKeepsTheFirst()
    {
        var list = new WorldCharacterList();
        var first = Entry(2, id: 1);
        list.Add(first);

        Assert.False(list.Add(Entry(2, id: 9)));
        Assert.Same(first, list[2]);
    }

    [Fact]
    public void RenameOccupiedSlotReplacesTheEntryWithARenamedCopy()
    {
        var list = new WorldCharacterList();
        var original = Entry(2, id: 7, name: "Old");
        list.Add(original);

        Assert.True(list.Rename(2, "New"));

        Assert.Equal("New", list[2]!.Name);
        Assert.NotSame(original, list[2]);
        Assert.Equal(7, list[2]!.Id);
        Assert.Equal(2, list[2]!.Slot);
    }

    [Fact]
    public void RenameEmptySlotReturnsFalse() => Assert.False(new WorldCharacterList().Rename(2, "New"));

    [Fact]
    public void ToArrayIsASnapshotAndDoesNotChangeWithLaterAdds()
    {
        var list = new WorldCharacterList();
        list.Add(Entry(0, id: 10));

        var snapshot = list.ToArray();
        list.Add(Entry(1, id: 20));

        Assert.Equal([10], [.. snapshot.Select(e => e.Id)]);
    }
}
