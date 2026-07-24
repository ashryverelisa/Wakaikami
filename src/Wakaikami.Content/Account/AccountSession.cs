using System.Net.Sockets;
using Microsoft.Extensions.Logging;
using Wakaikami.Networking.HandlerStores;
using Wakaikami.Networking.Session;

namespace Wakaikami.Content.Account;

public abstract class AccountSession(Socket pSocket, FiestaHandlerStore fiestaStore, ILogger logger) : FiestaSession(pSocket, logger, fiestaStore)
{
    public ushort SessionId { get; set; }
    public GameAccount? Account { get; set; }
}
