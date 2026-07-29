using System.Net.Sockets;
using Microsoft.Extensions.Logging;
using Wakaikami.Content.Account;
using Wakaikami.Content.Protocol.Misc.Server;
using Wakaikami.Networking.HandlerStores;
using Wakaikami.WorldService.Content.Entity.Character;
using Wakaikami.WorldService.GameNetwork.Protocol.Misc.Server;
using Wakaikami.WorldService.GameNetwork.Protocol.User.Server;

namespace Wakaikami.WorldService.GameNetwork.Listening;

public class WorldSession(Socket socket, FiestaHandlerStore fiestaStore, ILogger logger) : AccountSession(socket, fiestaStore, logger)
{
    public WorldCharacterList CharacterList { get; } = new();

    public WorldCharacter? Character { get; set; }

    public int ActiveCharacterId { get; set; }

    public override void SendHeartBeat() => SendPacket(new NcMiscHeartbeatAck());

    public override void SendHandShake() => SendPacket(new NcMiscSeedAck(Crypto.XorPosition));

    public override void Disconnect(bool notifyPeer)
    {
        if (notifyPeer)
        {
            SendPacket(new NcUserConnectionCutCmd());
        }
        Dispose();
    }
}
