using System.ComponentModel.DataAnnotations;

namespace Wakaikami.WorldService.Configuration;

public sealed class ZoneServerInfoOptions
{
    [Range(1, 1000)]
    public ushort MaxConnection { get; set; } = 10;
}