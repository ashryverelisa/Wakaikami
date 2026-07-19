using System.ComponentModel.DataAnnotations;

namespace Wakaikami.LoginService.Configuration;

public sealed class LoginServerInfoOptions
{
    [Range(1, 65535)]
    public ushort GameServerPort { get; set; } = 9010;

    [Range(1, 1000)]
    public ushort MaxConnection { get; set; } = 10;
}
