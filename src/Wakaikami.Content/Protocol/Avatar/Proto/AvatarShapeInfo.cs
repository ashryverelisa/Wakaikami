using Wakaikami.Content.Entity.Character;
using Wakaikami.Content.Entity.Character.Data;
using Wakaikami.Networking.Protocol.Fiesta;

namespace Wakaikami.Content.Protocol.Avatar.Proto;

public static class AvatarShapeInfo
{
    public static void Write(FiestaPacketWriter writer, CharacterShape shape) => Write(writer, shape.ToData());

    /// <summary>Overload for callers holding a detached appearance snapshot rather than a live entity.</summary>
    public static void Write(FiestaPacketWriter writer, CharacterShapeData shape)
    {
        writer.Write((byte)((0x01) | ((shape.ClassId & 0x1F) << 2) | ((shape.IsMale ? 1 : 0) << 7)));
        writer.Write(shape.HairTypeId);
        writer.Write(shape.HairColorId);
        writer.Write(shape.FaceId);
    }
}
