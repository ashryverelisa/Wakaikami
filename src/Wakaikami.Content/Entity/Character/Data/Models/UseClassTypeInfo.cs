using Wakaikami.Content.Entity.Character.Data.Models.Enums;
using Wakaikami.Core.IO.Shn;

namespace Wakaikami.Content.Entity.Character.Data.Models;

public class UseClassTypeInfo
{
    [ShnColumn("UseClass")]
    public UseClassType UseClass { get; set; }

    [ShnColumn("Fig")]
    public bool Fighter { get; set; }

    [ShnColumn("Cfig")]
    public bool CleverFighter { get; set; }

    [ShnColumn("War")]
    public bool Warrior { get; set; }

    [ShnColumn("Gla")]
    public bool Gladiator { get; set; }

    [ShnColumn("Kni")]
    public bool Knight { get; set; }

    [ShnColumn("Cle")]
    public bool Cleric { get; set; }

    [ShnColumn("Hcle")]
    public bool HighCleric { get; set; }

    [ShnColumn("Pal")]
    public bool Paladin { get; set; }

    [ShnColumn("Hol")]
    public bool HolyKnight { get; set; }

    [ShnColumn("Gua")]
    public bool Guardian { get; set; }

    [ShnColumn("Arc")]
    public bool Archer { get; set; }

    [ShnColumn("Harc")]
    public bool HawkArcher { get; set; }

    [ShnColumn("Sco")]
    public bool Scout { get; set; }

    [ShnColumn("Sha")]
    public bool SharpShooter { get; set; }

    [ShnColumn("Ran")]
    public bool Ranger { get; set; }

    [ShnColumn("Mag")]
    public bool Mage { get; set; }

    [ShnColumn("Wmag")]
    public bool WizMage { get; set; }

    [ShnColumn("Enc")]
    public bool Enchanter { get; set; }

    [ShnColumn("Warl")]
    public bool Warlock { get; set; }

    [ShnColumn("Wiz")]
    public bool Wizard { get; set; }

    [ShnColumn("Jok")]
    public bool Joker { get; set; }

    [ShnColumn("Chs")]
    public bool Chaser { get; set; }

    [ShnColumn("Cru")]
    public bool Cruel { get; set; }

    [ShnColumn("Cls")]
    public bool Closer { get; set; }

    [ShnColumn("Ass")]
    public bool Assassin { get; set; }

    [ShnColumn("Sen")]
    public bool Sentinel { get; set; }

    [ShnColumn("Sav")]
    public bool Savior { get; set; }
}
