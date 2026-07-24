namespace Wakaikami.WorldService.Content.Zone;

public class ZoneConnectInfo
{
    public ushort MaxPlayer { get; set; }
    public int NowPlayer { get; set; }

    public string ConnectIp { get; set; } = null!;

    public int ConnectPort { get; set; }

    public bool IsTestServer { get; set; }
}
