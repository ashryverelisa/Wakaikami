using Wakaikami.LoginService.Content.Account;

namespace Wakaikami.LoginService.Tests.Content.Account;

public sealed class AccountPresenceTests
{
    private static readonly PresenceOwner _world1 = PresenceOwner.World(1);
    private static readonly PresenceOwner _world2 = PresenceOwner.World(2);

    [Fact]
    public void TryReserveFreeAccountSucceeds()
    {
        Assert.True(new AccountPresence().TryReserve(1, PresenceOwner.Login));
    }

    [Fact]
    public void TryReserveSameAccountTwiceOnlyFirstWins()
    {
        var presence = new AccountPresence();

        Assert.True(presence.TryReserve(1, PresenceOwner.Login));
        Assert.False(presence.TryReserve(1, PresenceOwner.Login));
    }

    [Fact]
    public void TryReserveAccountHeldByAWorldFails()
    {
        var presence = new AccountPresence();
        presence.MarkOnline(1, _world1);

        Assert.False(presence.TryReserve(1, PresenceOwner.Login));
    }

    [Fact]
    public void TryReserveDifferentAccountsBothWin()
    {
        var presence = new AccountPresence();

        Assert.True(presence.TryReserve(1, PresenceOwner.Login));
        Assert.True(presence.TryReserve(2, PresenceOwner.Login));
    }

    [Fact]
    public void ReleaseIfOwnedByCurrentOwnerReleasesTheClaim()
    {
        var presence = new AccountPresence();
        presence.TryReserve(1, PresenceOwner.Login);

        Assert.True(presence.ReleaseIfOwnedBy(1, PresenceOwner.Login));
        Assert.True(presence.TryReserve(1, PresenceOwner.Login));
    }

    [Fact]
    public void ReleaseIfOwnedByFormerOwnerLeavesTheClaimAlone()
    {
        var presence = new AccountPresence();
        presence.MarkOnline(1, _world1);
        presence.MarkOnline(1, _world2);

        Assert.False(presence.ReleaseIfOwnedBy(1, _world1));
        Assert.False(presence.TryReserve(1, PresenceOwner.Login));
    }

    [Fact]
    public void ReleaseIfOwnedByUnclaimedAccountReturnsFalse()
    {
        Assert.False(new AccountPresence().ReleaseIfOwnedBy(99, PresenceOwner.Login));
    }

    [Fact]
    public void MarkOnlineClaimsWithoutGatingAndBlocksSubsequentReserve()
    {
        var presence = new AccountPresence();

        presence.MarkOnline(5, _world1);
        presence.MarkOnline(5, _world1);

        Assert.False(presence.TryReserve(5, PresenceOwner.Login));
    }

    [Fact]
    public void ReleaseWorldDropsOnlyThatWorldsClaims()
    {
        var presence = new AccountPresence();
        presence.MarkOnline(1, _world1);
        presence.MarkOnline(2, _world1);
        presence.MarkOnline(3, _world2);
        presence.TryReserve(4, PresenceOwner.Login);

        var released = presence.ReleaseWorld(1);

        Assert.Equal([1, 2], [.. released.Order()]);
        Assert.True(presence.TryReserve(1, PresenceOwner.Login));
        Assert.True(presence.TryReserve(2, PresenceOwner.Login));
        Assert.False(presence.TryReserve(3, PresenceOwner.Login));
        Assert.False(presence.TryReserve(4, PresenceOwner.Login));
    }

    [Fact]
    public void ReleaseWorldWithoutClaimsReturnsEmpty()
    {
        var presence = new AccountPresence();
        presence.TryReserve(1, PresenceOwner.Login);

        Assert.Empty(presence.ReleaseWorld(7));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(sbyte.MaxValue + 1)]
    public void TryWorldIdRegistrationWouldRejectIsNotAWorld(int worldId)
    {
        Assert.False(PresenceOwner.TryWorld(worldId, out _));
    }

    [Fact]
    public void TryWorldValidIdMapsToThatWorld()
    {
        Assert.True(PresenceOwner.TryWorld(3, out var owner));
        Assert.Equal(PresenceOwner.World(3), owner);
    }
}
