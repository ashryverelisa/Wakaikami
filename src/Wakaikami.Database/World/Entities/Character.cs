namespace Wakaikami.Database.World.Entities;

public class Character
{
    public int Id { get; set; }
    public int AccountId { get; set; }
    public required string Name { get; set; }
    public byte Slot { get; set; }
    public byte Level { get; set; }
    public long Money { get; set; }
    public int Hp { get; set; }
    public int Sp { get; set; }
    public int Lp { get; set; }
    public ushort HpStones { get; set; }
    public ushort SpStones { get; set; }
    public long Exp { get; set; }
    public int Fame { get; set; }
    public int PosX { get; set; }
    public int PosY { get; set; }
    public byte Direction { get; set; }
    public required string MapIndex { get; set; }
    public string? KqHandle { get; set; }
    public DateTime? KqDate { get; set; }
    public int LoginCount { get; set; }
    public byte IsRename { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastLogin { get; set; }
    public ushort FriendPoints { get; set; }
    public ushort LastFriendPoint { get; set; }
    public int KillCount { get; set; }

    // 1:1 sub-tables
    public CharacterShape? Shape { get; set; }
    public CharacterStatPoints? StatPoints { get; set; }
    public CharacterTutorial? Tutorial { get; set; }
    public CharacterOptions? Options { get; set; }
}
