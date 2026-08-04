using Wakaikami.Content.Account;
using Wakaikami.Core.Time;
using Wakaikami.Core.Updates;
using Wakaikami.LoginService.Content.Account;
using Wakaikami.LoginService.Content.Transfer;
using Wakaikami.LoginService.GameNetwork.Listening;
using Wakaikami.LoginService.Tests.TestSupport;

namespace Wakaikami.LoginService.Tests.Content.Transfer;

public sealed class AccountTransferManagerTests
{
    private static AccountTransferManager CreateManager() => new(new UpdateManager(), new AccountPresence());

    [Fact]
    public void GenerateTransferNewAccountReturnsTrue()
    {
        var account = Fakers.Account();

        var result = CreateManager().GenerateTransfer(account, session: null, out var transfer);

        Assert.True(result);
        Assert.NotNull(transfer);
        Assert.Equal(account, transfer.Account);
        Assert.NotEqual(Guid.Empty, transfer.AuthId);
    }

    [Fact]
    public void GenerateTransferDuplicateAccountReturnsFalse()
    {
        var transferManager = CreateManager();
        var account = Fakers.Account();

        transferManager.GenerateTransfer(account, session: null, out _);

        Assert.False(transferManager.GenerateTransfer(account, session: null, out _));
    }

    [Fact]
    public void IsTransferingExistingAccountReturnsTrue()
    {
        var transferManager = CreateManager();
        var account = Fakers.Account();
        transferManager.GenerateTransfer(account, session: null, out _);

        Assert.True(transferManager.IsTransfering(account));
    }

    [Fact]
    public void IsTransferingNonExistingAccountReturnsFalse() => Assert.False(CreateManager().IsTransfering(Fakers.Account()));

    [Fact]
    public void FinishTransferNonExistingAuthIdReturnsFalse()
    {
        var result = CreateManager().FinishTransfer(Guid.NewGuid(), out var transfer);

        Assert.False(result);
        Assert.Null(transfer);
    }

    [Fact]
    public void FinishTransferExistingTransferRemovesIt()
    {
        var transferManager = CreateManager();
        transferManager.GenerateTransfer(Fakers.Account(), session: null, out var created);

        var result = transferManager.FinishTransfer(created.AuthId, out var finished);

        Assert.True(result);
        Assert.NotNull(finished);
        Assert.Equal(created.AuthId, finished.AuthId);
    }

    [Fact]
    public void FinishTransferSameTransferTwiceSecondReturnsFalse()
    {
        var transferManager = CreateManager();
        transferManager.GenerateTransfer(Fakers.Account(), session: null, out var transfer);

        var first = transferManager.FinishTransfer(transfer.AuthId, out _);
        var second = transferManager.FinishTransfer(transfer.AuthId, out var replayed);

        Assert.True(first);
        Assert.False(second);
        Assert.Null(replayed);
    }

    [Fact]
    public void IsTransferingAfterFinishTransferReturnsFalse()
    {
        var transferManager = CreateManager();
        var account = Fakers.Account();
        transferManager.GenerateTransfer(account, session: null, out var transfer);
        transferManager.FinishTransfer(transfer.AuthId, out _);

        Assert.False(transferManager.IsTransfering(account));
    }

    [Fact]
    public void GenerateTransferAfterFinishTransferSameAccountCanTransferAgain()
    {
        var transferManager = CreateManager();
        var account = Fakers.Account();

        Assert.True(transferManager.GenerateTransfer(account, session: null, out var first));
        Assert.True(transferManager.FinishTransfer(first.AuthId, out _));
        Assert.True(transferManager.GenerateTransfer(account, session: null, out var second));

        Assert.NotEqual(first.AuthId, second.AuthId);
    }

    [Fact]
    public void GenerateTransferDifferentAccountsBothSucceed()
    {
        var transferManager = CreateManager();
        var first = Fakers.Account(id: 1);
        var second = Fakers.Account(id: 2);

        Assert.True(transferManager.GenerateTransfer(first, session: null, out var firstTransfer));
        Assert.True(transferManager.GenerateTransfer(second, session: null, out var secondTransfer));

        Assert.NotEqual(firstTransfer.AuthId, secondTransfer.AuthId);
        Assert.True(transferManager.IsTransfering(first));
        Assert.True(transferManager.IsTransfering(second));
    }

    [Fact]
    public void GenerateTransferAttachesTheSessionBeforePublishingTheTransfer()
    {
        var session = Reflection.Uninitialized<LoginSession>();

        Assert.True(CreateManager().GenerateTransfer(Fakers.Account(), session, out var transfer));

        Assert.Same(session, transfer.Session);
    }

    [Fact]
    public void GenerateTransferSetsExpiryHandle()
    {
        var transferManager = CreateManager();
        transferManager.GenerateTransfer(Fakers.Account(), session: null, out var transfer);

        Assert.NotNull(transfer.ExpiryHandle);
    }

    [Fact]
    public async Task OnExpiredTransferExpiresTheTokenIsNoLongerRedeemable()
    {
        var account = Fakers.Account(id: 98);

        var transferManager = await RunUntilExpiredAsync(new AccountPresence(), account);

        Assert.False(transferManager.IsTransfering(account));
    }

    [Fact]
    public async Task OnExpiredAfterExpiryTheAccountCanStartAFreshTransfer()
    {
        var account = Fakers.Account(id: 99);

        var transferManager = await RunUntilExpiredAsync(new AccountPresence(), account);

        Assert.True(transferManager.GenerateTransfer(account, session: null, out _));
    }

    [Fact]
    public async Task OnExpiredSessionAlreadyGoneReleasesThePresenceClaim()
    {
        // The account is claimed for the world it is about to move to; the session that held it is gone
        // (Session stays null here), so nothing but the expiry can ever hand the account back.
        var account = Fakers.Account(id: 97);
        var presence = new AccountPresence();
        Assert.True(presence.TryReserve(account.Id, PresenceOwner.Login));

        await RunUntilExpiredAsync(presence, account);

        Assert.True(presence.TryReserve(account.Id, PresenceOwner.Login));
    }

    private static async Task<AccountTransferManager> RunUntilExpiredAsync(AccountPresence presence, GameAccount account)
    {
        var future = DateTimeOffset.UtcNow.AddSeconds(30);
        using var updateManager = new UpdateManager(new ServerTimeProvider(new FrozenTimeProvider(future)));
        var transferManager = new AccountTransferManager(updateManager, presence);

        Assert.True(transferManager.GenerateTransfer(account, session: null, out var transfer));
        Assert.True(transferManager.IsTransfering(account));

        updateManager.Start();
        try
        {
            var deadline = DateTime.UtcNow.AddSeconds(5);
            while (transferManager.IsTransfering(account) && DateTime.UtcNow < deadline)
                await Task.Delay(50, TestContext.Current.CancellationToken);
        }
        finally
        {
            updateManager.Stop();
        }

        Assert.False(transferManager.IsTransfering(account));
        Assert.False(transferManager.FinishTransfer(transfer.AuthId, out _));
        return transferManager;
    }

    private sealed class FrozenTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }
}
