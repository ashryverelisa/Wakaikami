using Wakaikami.Networking.Protocol.Fiesta;

namespace Wakaikami.WorldService.GameNetwork.Protocol.Avatar.Client;

[GenerateSerialization]
public sealed partial class NcAvatarCreateReq(byte[] buffer) : FiestaClientPacket(buffer)
{
    [Field(0)]
    public byte Slot { get; private set; }

    [Field(1, Length = 20)]
    public string Name { get; private set; } = null!;

    [Field(2)]
    public byte ShapeInfo { get; private set; } // Race(2) + Class(5) + Gender(1)

    [Field(3)]
    public byte HairType { get; private set; }

    [Field(4)]
    public byte HairColor { get; private set; }

    [Field(5)]
    public byte FaceShape { get; private set; }

    public byte Race => (byte)(ShapeInfo & 0b00000011);

    public byte Class => (byte)((ShapeInfo >> 2) & 0b00011111);

    public bool IsMale => ((ShapeInfo >> 7) & 0b1) != 0;
}
