using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Microsoft.Extensions.Logging;
using Wakaikami.Content.Account;
using Wakaikami.LoginService.Content.Account;
using Wakaikami.LoginService.Content.Account.Interfaces;
using Wakaikami.LoginService.Content.Transfer;
using Wakaikami.Networking.Grpc.Messages.WorldLoginAccount;

namespace Wakaikami.LoginService.Grpc;

public sealed partial class WorldLoginAccountService(
    IAccountManager accounts,
    IAccountPresence presence,
    AccountTransferManager transfers,
    ILogger<WorldLoginAccountService> logger
) : WorldLoginAccount.WorldLoginAccountBase
{
    public override async Task<Empty> AccountOffline(AccountOfflineRequest request, ServerCallContext context)
    {
        // Only the world that still holds the account may release it: a notice that overtakes a handover
        // (world -> world, or world -> login) would otherwise clear a claim that is no longer its own.
        if (TryWorld(request.WorldId, out var owner))
            presence.ReleaseIfOwnedBy(request.AccountId, owner);

        await accounts.UpdateAccountStateAsync(request.AccountId, false, context.CancellationToken);
        return new Empty();
    }

    /// <summary>Bulk resync a world pushes when it (re)connects: these accounts are in-game right now.</summary>
    public override async Task<Empty> AccountOnlineList(AccountOnlineListRequest request, ServerCallContext context)
    {
        var known = TryWorld(request.WorldId, out var owner);

        foreach (var accountId in request.AccountIds)
        {
            if (known)
                presence.MarkOnline(accountId, owner);

            await accounts.UpdateAccountStateAsync(accountId, isOnline: true, context.CancellationToken);
        }
        return new Empty();
    }

    private bool TryWorld(int worldId, out PresenceOwner owner)
    {
        if (PresenceOwner.TryWorld(worldId, out owner))
            return true;

        LogInvalidWorldId(worldId);
        return false;
    }

    public override Task<TransferGenReply> TransferGen(TransferGenRequest request, ServerCallContext context)
    {
        var reply = transfers.GenerateTransfer(new GameAccount { Id = request.AccountId, Name = request.AccountName }, out var transfer)
            ? new TransferGenReply { AuthId = transfer.AuthId.ToString(), Success = true }
            : new TransferGenReply { Success = false };
        return Task.FromResult(reply);
    }

    public override Task<TransferGetReply> TransferGet(TransferGetRequest request, ServerCallContext context)
    {
        if (Guid.TryParse(request.TransferId, out var transferId) && transfers.FinishTransfer(transferId, out var transfer))
        {
            // Fetching the transfer is the handover: from here the requesting world holds the account, so a
            // crash on its side drops the claim and the previous holder can no longer release it.
            if (TryWorld(request.WorldId, out var owner))
                presence.MarkOnline(transfer.Account.Id, owner);

            return Task.FromResult(
                new TransferGetReply
                {
                    Found = true,
                    AccountId = transfer.Account.Id,
                    AccountName = transfer.Account.Name,
                }
            );
        }

        return Task.FromResult(new TransferGetReply { Found = false });
    }

    [LoggerMessage(EventId = 1, Level = LogLevel.Warning, Message = "Account report carried an invalid world id {WorldId}; presence left unchanged")]
    private partial void LogInvalidWorldId(int worldId);
}
