using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Options;

namespace Wakaikami.ZoneService.Configuration;

public sealed class ZoneOptions
{
    public const string SectionName = "Zone";

    [Required, ValidateObjectMembers]
    public WorldConnectInfoOptions WorldConnectInfo { get; set; } = new();

    [Required, ValidateObjectMembers]
    public ZoneServerInfoOptions ZoneServerInfo { get; set; } = new();
}
