using System.ComponentModel.DataAnnotations;

namespace Wakaikami.ZoneService.Configuration;

public sealed class WorldConnectInfoOptions
{
    [Range(1, 3600)]
    public int Syncinterval { get; set; } = 30;
}