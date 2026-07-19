using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Options;

namespace Wakaikami.WorldService.Configuration;

public sealed class ZoneInfoOptions
{
    [Required, ValidateObjectMembers]
    public ZoneServerInfoOptions ServerInfo { get; set; } = new();
}
