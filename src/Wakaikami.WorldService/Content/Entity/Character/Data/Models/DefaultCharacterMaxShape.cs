using Wakaikami.Core.IO.Shine;

namespace Wakaikami.WorldService.Content.Entity.Character.Data.Models;

public class DefaultCharacterMaxShape
{
    [ShineTableColumn("Class")]
    public byte Class { get; set; }

    [ShineTableColumn("Max Face type")]
    public byte MaxFace { get; set; }

    [ShineTableColumn("Max Hair type")]
    public byte MaxHair { get; set; }

    [ShineTableColumn("Max Hair color")]
    public byte MaxHairColor { get; set; }
}