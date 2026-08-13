using Wakaikami.Core.IO.Shine;

namespace Wakaikami.WorldService.Content.Entity.Character.Data.Models;

public class DefaultCharacterItem
{
    [ShineTableColumn("Class")]
    public byte Class { get; set; }

    [ShineTableColumn("Item ID")]
    public ushort ItemId { get; set; }

    [ShineTableColumn("Item Lot")]
    public byte ItemAmount { get; set; }
}
