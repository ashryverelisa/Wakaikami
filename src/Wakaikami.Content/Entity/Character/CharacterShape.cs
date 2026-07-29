using Wakaikami.Content.Entity.Character.Data;

namespace Wakaikami.Content.Entity.Character;

public class CharacterShape
{
    public byte ClassId { get; private set; }
    public byte RaceId { get; private set; }
    public bool IsMale { get; private set; }
    public byte HairTypeId { get; private set; }
    public byte HairColorId { get; private set; }
    public byte FaceId { get; private set; }

    public void Apply(CharacterShapeData data)
    {
        ClassId = data.ClassId;
        RaceId = data.RaceId;
        IsMale = data.IsMale;
        HairTypeId = data.HairTypeId;
        HairColorId = data.HairColorId;
        FaceId = data.FaceId;
    }

    public CharacterShapeData ToData() => new(ClassId, RaceId, IsMale, HairTypeId, HairColorId, FaceId);

    public void SetClassId(byte classId) => ClassId = classId;
}
