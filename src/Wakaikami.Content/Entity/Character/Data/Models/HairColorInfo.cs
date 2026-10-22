using Wakaikami.Content.Entity.Character.Data.Models.Enums;
using Wakaikami.Core.IO.Shn;

namespace Wakaikami.Content.Entity.Character.Data.Models;

public class HairColorInfo
{
    [ShnColumn("ID")]
    public byte Id { get; set; }

    [ShnColumn("IndexName")]
    public string Index { get; set; } = string.Empty;

    [ShnColumn("Name")]
    public string Name { get; set; } = string.Empty;

    [ShnColumn("Grade")]
    public BodyShopGrade Grade { get; set; }
}
