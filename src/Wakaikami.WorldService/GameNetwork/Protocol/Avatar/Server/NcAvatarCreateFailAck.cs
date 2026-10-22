using Wakaikami.Networking.HandlerTypes;
using Wakaikami.Networking.Protocol.Fiesta;

namespace Wakaikami.WorldService.GameNetwork.Protocol.Avatar.Server;

[GenerateSerialization]
public partial class NcAvatarCreateFailAck([Field(0, As = typeof(short))] AvatarErrors error)
    : FiestaServerPacket(FiestaHandlerType.Avatar, GameHandler05Type.NcAvatarCreateFailAck);
