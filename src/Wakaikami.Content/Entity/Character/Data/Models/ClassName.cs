using Wakaikami.Core.IO.Shn;

namespace Wakaikami.Content.Entity.Character.Data.Models;

public class ClassName
{
    [ShnColumn("ClassID")]
    public byte Id { get; set; }

    [ShnColumn("acPrefix")]
    public string Prefix { get; set; } = string.Empty;

    [ShnColumn("acEngName")]
    public string EngName { get; set; } = string.Empty;

    [ShnColumn("acLocalName")]
    public string LocalName { get; set; } = string.Empty;
}
