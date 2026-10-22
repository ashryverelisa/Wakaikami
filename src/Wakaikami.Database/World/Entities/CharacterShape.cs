namespace Wakaikami.Database.World.Entities;

public class CharacterShape
{
    public int CharacterId { get; set; }
    public byte Class { get; set; }
    public byte Race { get; set; }
    public byte IsMale { get; set; }
    public byte Hair { get; set; }
    public byte HairColor { get; set; }
    public byte Face { get; set; }

    public Character Character { get; set; } = null!;
}