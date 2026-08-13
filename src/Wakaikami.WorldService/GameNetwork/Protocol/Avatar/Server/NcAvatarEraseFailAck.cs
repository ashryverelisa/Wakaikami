using Wakaikami.Networking.HandlerTypes;
using Wakaikami.Networking.Protocol.Fiesta;

namespace Wakaikami.WorldService.GameNetwork.Protocol.Avatar.Server;

[GenerateSerialization]
public partial class NcAvatarEraseFailAck([Field(0, As = typeof(short))] AvatarErrors code)
    : FiestaServerPacket(FiestaHandlerType.Avatar, GameHandler05Type.NcAvatarEraseFailAck);