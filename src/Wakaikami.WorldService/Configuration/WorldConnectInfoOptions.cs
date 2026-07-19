using System.ComponentModel.DataAnnotations;

namespace Wakaikami.WorldService.Configuration;

/// <summary>World→Login gRPC sync interval (authenticated via mTLS).</summary>
public sealed class WorldConnectInfoOptions
{
    [Range(1, 3600)]
    public int Syncinterval { get; set; } = 30;
}
