using System.Net.Sockets;
using Microsoft.Extensions.Logging;
using Wakaikami.Networking.Cryptography;
using Wakaikami.Networking.HandlerStores;
using Wakaikami.Networking.Protocol.Fiesta;

namespace Wakaikami.Networking.Session;

public abstract class FiestaSession : SessionBase
{
    public FiestaCryptoProvider Crypto { get; private set; }

    protected FiestaSession(Socket pSocket, ILogger logger, FiestaHandlerStore fiestaStore)
        : base(pSocket, logger)
    {
        Crypto = new FiestaCryptoProvider();
        DataParser = new FiestaPacketParser(this, Logger, fiestaStore);
    }

    public override void SendPacket<TPacket>(TPacket packet, bool destroy = true)
    {
        if (IsDisposed)
            return;

        if (packet is FiestaServerPacket { Writer.Length: 0 })
        {
            packet.Write();
        }

        base.SendPacket(packet, destroy);
    }

    public abstract void SendHandShake();

    public virtual void SendHeartBeat() { }

    public abstract void Disconnect(bool notifyPeer);
}
