using Wakaikami.Networking.Protocol.Fiesta;

namespace Wakaikami.WorldService.GameNetwork.Protocol.Avatar.Client;

[GenerateSerialization]
public sealed partial class NcAvatarEraseReq(byte[] buffer) : FiestaClientPacket(buffer)
{
    [Field(0)]
    public byte Slot { get; private set; }
}
