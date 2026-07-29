using Wakaikami.Networking.Protocol.Fiesta;

namespace Wakaikami.Content.Protocol.Avatar.Proto;

public static class AvatarDeleteInfo
{
    public static void Write(FiestaPacketWriter writer, DateTime date)
    {
        writer.Write((byte)date.Year);
        writer.Write((byte)date.Month);
        writer.Write((byte)date.Day);
        writer.Write((byte)date.Hour);
        writer.Write((byte)date.Minute);
    }
}
