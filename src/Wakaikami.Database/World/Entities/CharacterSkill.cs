namespace Wakaikami.Database.World.Entities;

public class CharacterSkill
{
    public int CharacterId { get; set; }
    public ushort SkillId { get; set; }

    public int CoolTime { get; set; }

    public byte EmpowerDamage { get; set; }
    public byte EmpowerSp { get; set; }
    public byte EmpowerKeepTime { get; set; }
    public byte EmpowerCoolTime { get; set; }
    public int Mastery { get; set; }

    public Character Character { get; set; } = null!;
}
