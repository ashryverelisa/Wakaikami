using Wakaikami.Networking.HandlerTypes;
using Wakaikami.Networking.Protocol.Fiesta;
using Wakaikami.Networking.Session;

namespace Wakaikami.Networking.HandlerStores.Interfaces;

public interface IGamePacketHandler
{
    public FiestaHandlerType HandlerType { get; }
    public ushort Opcode { get; }
    public FiestaClientPacket CreatePacket(byte[] data);
    public Task Handle(IServiceProvider services, FiestaSession session, FiestaClientPacket packet);
}
