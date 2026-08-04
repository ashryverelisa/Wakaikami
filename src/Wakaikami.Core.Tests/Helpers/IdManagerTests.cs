using Wakaikami.Core.Helpers;

namespace Wakaikami.Core.Tests.Helpers;

public sealed class IdManagerTests
{
    [Fact]
    public void ConstructorMinGreaterThanMaxThrows()
    {
        Assert.Throws<ArgumentException>(() => new IdManager(10, 5));
    }

    [Fact]
    public void ConstructorMinEqualsMaxAllowsSingleId()
    {
        var manager = new IdManager(7, 7);

        Assert.True(manager.TryGetNewId(out var id));
        Assert.Equal(7, id);
        Assert.False(manager.TryGetNewId(out _));
    }

    [Fact]
    public void TryGetNewIdHandsOutIdsStartingFromMin()
    {
        var manager = new IdManager(1, 3);

        Assert.True(manager.TryGetNewId(out var first));
        Assert.True(manager.TryGetNewId(out var second));
        Assert.True(manager.TryGetNewId(out var third));

        Assert.Equal(1, first);
        Assert.Equal(2, second);
        Assert.Equal(3, third);
    }

    [Fact]
    public void TryGetNewIdWhenExhaustedReturnsFalseAndZero()
    {
        var manager = new IdManager(1, 1);
        manager.TryGetNewId(out _);

        var result = manager.TryGetNewId(out var id);

        Assert.False(result);
        Assert.Equal(0, id);
    }

    [Fact]
    public void TryReleaseIdMakesIdAvailableAgain()
    {
        var manager = new IdManager(1, 1);
        manager.TryGetNewId(out var id);

        Assert.True(manager.TryReleaseId(id));
        Assert.True(manager.TryGetNewId(out var reused));
        Assert.Equal(id, reused);
    }

    [Fact]
    public void TryReleaseIdDuplicateReleaseReturnsFalse()
    {
        var manager = new IdManager(1, 5);
        manager.TryGetNewId(out var id);

        Assert.True(manager.TryReleaseId(id));
        Assert.False(manager.TryReleaseId(id));
    }

    [Fact]
    public void TryReleaseIdIdStillInPoolReturnsFalse()
    {
        var manager = new IdManager(1, 5);

        Assert.False(manager.TryReleaseId(3));
    }
}
