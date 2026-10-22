using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Wakaikami.Core.Updates;
using Wakaikami.LoginService.Content.Account;
using Wakaikami.LoginService.Content.Account.Interfaces;
using Wakaikami.LoginService.Content.Transfer;
using Wakaikami.LoginService.Grpc;
using Wakaikami.LoginService.Tests.TestSupport;
using Wakaikami.Networking.Grpc.Messages.WorldLoginAccount;

namespace Wakaikami.LoginService.Tests.Grpc;

public sealed class WorldLoginAccountServiceTests
{
    private const int WorldId = 3;
    private static readonly PresenceOwner _world = PresenceOwner.World(WorldId);

    private readonly IAccountManager _accounts = Substitute.For<IAccountManager>();
    private readonly IAccountPresence _presence = Substitute.For<IAccountPresence>();
    private readonly AccountTransferManager _transfers = new(new UpdateManager(), Substitute.For<IAccountPresence>());

    private WorldLoginAccountService GetWorldLoginAccountService() => new(_presence, _transfers, NullLogger<WorldLoginAccountService>.Instance);

    [Fact]
    public async Task AccountOfflineReleasesTheReportingWorldsClaim()
    {
        await GetWorldLoginAccountService()
            .AccountOffline(new AccountOfflineRequest { AccountId = 42, WorldId = WorldId }, GrpcTest.Context(TestContext.Current.CancellationToken));

        _presence.Received(1).ReleaseIfOwnedBy(42, _world);
    }

    [Fact]
    public async Task AccountOfflineInvalidWorldIdLeavesPresenceAlone()
    {
        await GetWorldLoginAccountService()
            .AccountOffline(new AccountOfflineRequest { AccountId = 42 }, GrpcTest.Context(TestContext.Current.CancellationToken));

        _presence.DidNotReceive().ReleaseIfOwnedBy(Arg.Any<int>(), Arg.Any<PresenceOwner>());
    }

    [Fact]
    public async Task AccountOnlineListRecordsTheReportingWorldAsHolder()
    {
        await GetWorldLoginAccountService()
            .AccountOnlineList(
                new AccountOnlineListRequest { AccountIds = { 1, 2, 3 }, WorldId = WorldId },
                GrpcTest.Context(TestContext.Current.CancellationToken)
            );

        _presence.Received(1).MarkOnline(1, _world);
        _presence.Received(1).MarkOnline(2, _world);
        _presence.Received(1).MarkOnline(3, _world);
    }

    [Fact]
    public async Task AccountOnlineListInvalidWorldIdLeavesPresenceAlone()
    {
        await GetWorldLoginAccountService()
            .AccountOnlineList(new AccountOnlineListRequest { AccountIds = { 1, 2, 3 } }, GrpcTest.Context(TestContext.Current.CancellationToken));

        _presence.DidNotReceive().MarkOnline(Arg.Any<int>(), Arg.Any<PresenceOwner>());
    }

    [Fact]
    public async Task AccountOnlineListEmptyListTouchesNothing()
    {
        await GetWorldLoginAccountService()
            .AccountOnlineList(new AccountOnlineListRequest { WorldId = WorldId }, GrpcTest.Context(TestContext.Current.CancellationToken));

        _presence.DidNotReceive().MarkOnline(Arg.Any<int>(), Arg.Any<PresenceOwner>());
    }

    [Fact]
    public async Task TransferGenSuccessReturnsAuthIdAndSuccessTrue()
    {
        var result = await GetWorldLoginAccountService()
            .TransferGen(new TransferGenRequest { AccountId = 42, AccountName = "Hero" }, GrpcTest.Context(TestContext.Current.CancellationToken));

        Assert.True(result.Success);
        Assert.True(Guid.TryParse(result.AuthId, out var authId));
        Assert.NotEqual(Guid.Empty, authId);
    }

    [Fact]
    public async Task TransferGenDuplicateFailsReturnsSuccessFalse()
    {
        var service = GetWorldLoginAccountService();
        var request = new TransferGenRequest { AccountId = 42, AccountName = "Hero" };

        await service.TransferGen(request, GrpcTest.Context(TestContext.Current.CancellationToken)); // first succeeds
        var result = await service.TransferGen(request, GrpcTest.Context(TestContext.Current.CancellationToken)); // duplicate

        Assert.False(result.Success);
        Assert.Equal(string.Empty, result.AuthId);
    }

    [Fact]
    public async Task TransferGetExistingTransferReturnsFoundAndHandsTheAccountToThatWorld()
    {
        var service = GetWorldLoginAccountService();
        var generated = await service.TransferGen(
            new TransferGenRequest { AccountId = 7, AccountName = "Mage" },
            GrpcTest.Context(TestContext.Current.CancellationToken)
        );

        var fetched = await service.TransferGet(
            new TransferGetRequest { TransferId = generated.AuthId, WorldId = WorldId },
            GrpcTest.Context(TestContext.Current.CancellationToken)
        );

        Assert.True(fetched.Found);
        Assert.Equal(7, fetched.AccountId);
        Assert.Equal("Mage", fetched.AccountName);
        _presence.Received(1).MarkOnline(7, _world);
    }

    [Fact]
    public async Task TransferGetSameTokenTwiceSecondIsNotFound()
    {
        var service = GetWorldLoginAccountService();
        var generated = await service.TransferGen(
            new TransferGenRequest { AccountId = 7, AccountName = "Mage" },
            GrpcTest.Context(TestContext.Current.CancellationToken)
        );
        var request = new TransferGetRequest { TransferId = generated.AuthId, WorldId = WorldId };

        await service.TransferGet(request, GrpcTest.Context(TestContext.Current.CancellationToken));
        var replayed = await service.TransferGet(request, GrpcTest.Context(TestContext.Current.CancellationToken));

        Assert.False(replayed.Found);
    }

    [Fact]
    public async Task TransferGetUnknownTransferReturnsNotFound()
    {
        var result = await GetWorldLoginAccountService()
            .TransferGet(new TransferGetRequest { TransferId = Guid.NewGuid().ToString() }, GrpcTest.Context(TestContext.Current.CancellationToken));

        Assert.False(result.Found);
    }

    [Fact]
    public async Task TransferGetMalformedTransferIdReturnsNotFound()
    {
        var result = await GetWorldLoginAccountService()
            .TransferGet(new TransferGetRequest { TransferId = "not-a-guid" }, GrpcTest.Context(TestContext.Current.CancellationToken));

        Assert.False(result.Found);
    }
}
