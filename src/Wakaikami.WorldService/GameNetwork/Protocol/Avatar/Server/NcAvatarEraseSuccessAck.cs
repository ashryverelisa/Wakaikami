using Wakaikami.Networking.HandlerTypes;
using Wakaikami.Networking.Protocol.Fiesta;

namespace Wakaikami.WorldService.GameNetwork.Protocol.Avatar.Server;

[GenerateSerialization]
public partial class NcAvatarEraseSuccessAck([Field(0)] byte slot) : FiestaServerPacket(FiestaHandlerType.Avatar, GameHandler05Type.NcAvatarEraseSuccessAck);