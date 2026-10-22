using Wakaikami.Core.IO.Shn;

namespace Wakaikami.Content.TextFilter.Data.Models;

public class BadNameFilter
{
    [ShnColumn("BadName")]
    public string Name { get; set; } = string.Empty;
}
