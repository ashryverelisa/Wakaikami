namespace Wakaikami.Database.World.Entities;

public class CharacterOptions
{
    public int CharacterId { get; set; }
    public byte[]? SShortCutData { get; set; }
    public byte[]? SShortCutSize { get; set; }
    public byte[]? SVideo { get; set; }
    public byte[]? SSound { get; set; }
    public byte[]? SGame { get; set; }
    public byte[]? SWindowsPos { get; set; }
    public byte[]? SKeyMapping { get; set; }

    public Character Character { get; set; } = null!;
}