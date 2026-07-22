using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using Wakaikami.Content.Account;
using Wakaikami.Core.Time;
using Wakaikami.Core.Updates;
using Wakaikami.LoginService.Content.Account;
using Wakaikami.LoginService.Content.Account.Interfaces;
using Wakaikami.LoginService.GameNetwork.Protocol.User;
using Wakaikami.LoginService.GameNetwork.Protocol.User.Server;

namespace Wakaikami.LoginService.Content.Transfer;

public sealed class AccountTransferManager(UpdateManager updateManager, IAccountPresence presence)
{
    private const int TransferTimeoutSeconds = 10;

    private readonly ConcurrentDictionary<Guid, AccountTransfer> _transfersByGuid = new();
    private readonly ConcurrentDictionary<int, AccountTransfer> _transfersById = new();

    public bool GenerateTransfer(GameAccount account, out AccountTransfer tf)
    {
        tf = new AccountTransfer(account);

        if (!_transfersById.TryAdd(account.Id, tf))
            return false;

        if (_transfersByGuid.TryAdd(tf.AuthId, tf))
        {
            var captured = tf;
            tf.ExpiryHandle = updateManager.ScheduleExpiry(ServerClock.UtcNow.AddSeconds(TransferTimeoutSeconds), () => OnExpired(captured));
            return true;
        }

        Remove(tf);
        return false;
    }

    public bool IsTransfering(GameAccount account) => _transfersById.ContainsKey(account.Id);

    public bool FinishTransfer(Guid authId, [NotNullWhen(true)] out AccountTransfer? transfer)
    {
        if (!_transfersByGuid.TryRemove(authId, out transfer))
            return false;

        Remove(transfer);
        return true;
    }

    private void OnExpired(AccountTransfer tf)
    {
        if (!FinishTransfer(tf.AuthId, out _))
            return;

        if (tf.Session?.IsConnected != true)
            return;

        if (tf.Session?.IsConnected == true)
        {
            tf.Session.SendPacket(new NcUserLoginFailAck(UserErrors.LoginTimeout));
            tf.Session.Dispose();
            return;
        }

        presence.ReleaseIfOwnedBy(tf.Account.Id, PresenceOwner.Login);
    }

    private void Remove(AccountTransfer tf)
    {
        _transfersById.TryRemove(tf.Account.Id, out _);
        _transfersByGuid.TryRemove(tf.AuthId, out _);
        tf.ExpiryHandle?.Dispose();
    }
}
