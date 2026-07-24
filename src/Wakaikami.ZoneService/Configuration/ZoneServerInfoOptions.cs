using System.ComponentModel.DataAnnotations;

namespace Wakaikami.ZoneService.Configuration;

public sealed class ZoneServerInfoOptions
{
    [Range(1, 65535)]
    public ushort ListeningPort { get; set; } = 9230;

    [Range(1, 1000)]
    public ushort MaxConnection { get; set; } = 10;

    [Required]
    public string ConnectIp { get; set; } = "127.0.0.1";

    [Range(1, 10000)]
    public ushort MaxPlayers { get; set; } = 10;

    public bool IsTestServer { get; set; }

    public byte ZoneId { get; set; }
}