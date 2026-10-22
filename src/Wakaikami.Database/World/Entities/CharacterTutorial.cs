namespace Wakaikami.Database.World.Entities;

public class CharacterTutorial
{
    public int CharacterId { get; set; }
    public byte State { get; set; }
    public byte Step { get; set; }
    public DateTime Date { get; set; }

    public Character Character { get; set; } = null!;
}