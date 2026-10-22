using Wakaikami.Core.IO.Shine;

namespace Wakaikami.WorldService.Content.Entity.Character.Data.Models;

public class DefaultCharacterShortcut
{
    [ShineTableColumn("Class")]
    public byte Class { get; set; }

    [ShineTableColumn("nSlotNo")]
    public byte Slot { get; set; }

    [ShineTableColumn("nCodeNo")]
    public ushort Code { get; set; }

    [ShineTableColumn("nValue")]
    public ushort Value { get; set; }
}