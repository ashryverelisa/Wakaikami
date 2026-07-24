using System.Net.Sockets;
using Microsoft.Extensions.Logging;
using Wakaikami.Networking.HandlerStores;
using Wakaikami.Networking.Session;
using Wakaikami.ZoneService.Content.Entity.Character;
using Wakaikami.ZoneService.GameNetwork.Protocol.Misc.Server;
using Wakaikami.ZoneService.GameNetwork.Protocol.UserConnection.Server;

namespace Wakaikami.ZoneService.GameNetwork.Listening;

public class ZoneSession(Socket socket, ILogger logger, FiestaHandlerStore fiestaStore) : FiestaSession(socket, logger, fiestaStore)
{
    public ZoneCharacter? Character { get; set; }
    public ushort RegisteredSessionId { get; set; }
    public bool Logout { get; set; }

    public override void Disconnect(bool notifyPeer)
    {
        if (notifyPeer)
        {
            SendPacket(new NcUserConnectionCutCmd());
        }
        Dispose();
    }

    public override void SendHandShake() => SendPacket(new NcMiscSeedAck(Crypto.XorPosition));

    public override void SendHeartBeat() => SendPacket(new NcHeartbeatAck());
}
