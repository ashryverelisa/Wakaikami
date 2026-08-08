namespace Wakaikami.Database.World.Entities;

public class CharacterStatPoints
{
    public int CharacterId { get; set; }
    public byte Points { get; set; }
    public byte Spr { get; set; }
    public byte Int { get; set; }
    public byte Dex { get; set; }
    public byte Str { get; set; }
    public byte End { get; set; }

    /// <summary>Stat-reset ("redistribute") points available to the character.</summary>
    public byte RedistributePoint { get; set; }

    public Character Character { get; set; } = null!;
}