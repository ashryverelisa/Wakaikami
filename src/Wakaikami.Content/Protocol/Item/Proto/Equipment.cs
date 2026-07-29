using Wakaikami.Networking.Protocol.Fiesta;

namespace Wakaikami.Content.Protocol.Item.Proto;

public static class Equipment
{
    // TODO
    public static void Write(FiestaPacketWriter packet)
    {
        packet.Write(ushort.MaxValue); // Head // Equip=1
        packet.Write(ushort.MaxValue); // Equ_Mouth // Equip=24
        packet.Write(ushort.MaxValue); // Equ_RightHand
        packet.Write(ushort.MaxValue); // Equ_Body
        packet.Write(ushort.MaxValue); // Equ_LeftHand
        packet.Write(ushort.MaxValue); // Equ_Pant
        packet.Write(ushort.MaxValue); // Equ_Boot
        packet.Write(ushort.MaxValue); // Equ_AccBoot
        packet.Write(ushort.MaxValue); // Equ_AccPant
        packet.Write(ushort.MaxValue); // Equ_AccBody
        packet.Write(ushort.MaxValue); // Equ_AccHeadA
        packet.Write(ushort.MaxValue); // Equ_MiniMon_R
        packet.Write(ushort.MaxValue); // Equ_Eye
        packet.Write(ushort.MaxValue); // Equ_AccLeftHand
        packet.Write(ushort.MaxValue); // Equ_AccRightHand
        packet.Write(ushort.MaxValue); // Equ_AccBack
        packet.Write(ushort.MaxValue); // Equ_CosEff
        packet.Write(ushort.MaxValue); // Equ_AccHip
        packet.Write(ushort.MaxValue); // Equ_Minimon
        packet.Write(ushort.MaxValue); // Equ_AccShield
    }

    public static void WriteUpgrade(FiestaPacketWriter packet)
    {
        packet.Write<byte>(0xff);
        packet.Write<byte>(0xff);
        packet.Write<byte>(0xff);
    }
}
