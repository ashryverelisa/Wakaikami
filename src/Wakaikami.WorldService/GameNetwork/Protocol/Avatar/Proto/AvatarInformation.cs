using Wakaikami.Content.Protocol.Avatar.Proto;
using Wakaikami.Content.Protocol.Item.Proto;
using Wakaikami.Core.Time;
using Wakaikami.Networking.Protocol.Abstractions;
using Wakaikami.Networking.Protocol.Fiesta;
using Wakaikami.WorldService.Content.Entity.Character;

namespace Wakaikami.WorldService.GameNetwork.Protocol.Avatar.Proto;

public static class AvatarInformation
{
    public static void Write(FiestaPacketWriter writer, CharacterListEntry character)
    {
        var mapIndex = character.MapIndex;

        writer.Write(character.Id);
        writer.WriteString(character.Name, 20);
        writer.Write(character.Level);
        writer.Write(character.Slot);
        writer.WriteString(mapIndex, 12);
        AvatarDeleteInfo.Write(writer, ServerClock.LocalNow);
        AvatarShapeInfo.Write(writer, character.Shape);

        Equipment.Write(writer);

        writer.Write((byte)0x00);
        writer.Write((byte)0x00);
        writer.Write((byte)0x00);

        writer.Write((uint)character.InstanceId);
        writer.WriteString(mapIndex, 12);
        writer.Write((uint)character.Position.X);
        writer.Write((uint)character.Position.Y);
        writer.Write(0); //KQ Date

        NeedNameChanged(writer, false);

        writer.Write((int)0); //tutorial state
        writer.Write((byte)0); // tutorial step
        writer.Fill(0x0, 1); //unk
    }

    private static void NeedNameChanged(PacketWriter writer, bool needChanged)
    {
        writer.Write(needChanged);
        writer.Write((byte)(needChanged ? 0 : 100));
        writer.Write((byte)0);
        writer.Write((byte)0);
        writer.Write((byte)0);
    }
}
