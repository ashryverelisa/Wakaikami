using Wakaikami.Networking.HandlerTypes;
using Wakaikami.Networking.Protocol.Fiesta;

namespace Wakaikami.Networking.HandlerStores.Registration;

public sealed record GamePacketTypeBinding(FiestaHandlerType HandlerType, ushort Opcode, Func<byte[], FiestaClientPacket> Factory);
