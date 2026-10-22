using Wakaikami.Core.IO.Shine;

namespace Wakaikami.WorldService.Content.Entity.Character.Data.Models;

public class DefaultCharacterSkill
{
    [ShineTableColumn("Class")]
    public byte Class { get; set; }

    [ShineTableColumn("Skill ID")]
    public ushort SkillId { get; set; }
}