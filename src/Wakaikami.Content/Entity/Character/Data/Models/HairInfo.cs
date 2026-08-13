using Wakaikami.Content.Entity.Character.Data.Models.Enums;
using Wakaikami.Core.IO.Shn;

namespace Wakaikami.Content.Entity.Character.Data.Models;

public class HairInfo
{
    [ShnColumn("ID")]
    public byte Id { get; set; }

    [ShnColumn("IndexName")]
    public string Index { get; set; } = string.Empty;

    [ShnColumn("HairName")]
    public string Name { get; set; } = string.Empty;

    [ShnColumn("Grade")]
    public BodyShopGrade Grade { get; set; }
}