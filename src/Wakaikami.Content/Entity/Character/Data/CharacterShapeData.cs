namespace Wakaikami.Content.Entity.Character.Data;

public readonly record struct CharacterShapeData(byte ClassId, byte RaceId, bool IsMale, byte HairTypeId, byte HairColorId, byte FaceId);
