using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Options;

namespace Wakaikami.WorldService.Configuration;

public sealed class WorldInfoOptions
{
    /// <summary>Shown in the client's world list; the packet field is 16 bytes.</summary>
    [Required, StringLength(16)]
    public string WorldName { get; set; } = "";

    /// <summary>Client-facing connect address, announced to Login at registration.</summary>
    [Required, StringLength(15)]
    public string ConnectIp { get; set; } = "127.0.0.1";

    [Range(1, 65535)]
    public ushort ListeningPort { get; set; } = 9120;

    public bool IsTestServer { get; set; }

    [Range(1, 1000)]
    public ushort MaxConnection { get; set; } = 10;

    [Range(1, 10000)]
    public ushort MaxPlayer { get; set; } = 12;

    [Range(1, 3600)]
    public ushort SyncTime { get; set; } = 300;

    [Range(1, 3600)]
    public ushort SyncTimeOut { get; set; } = 600;

    public sbyte WorldId { get; set; }

    [Required, ValidateObjectMembers]
    public WorldConnectInfoOptions ConnectInfo { get; set; } = new();
}
