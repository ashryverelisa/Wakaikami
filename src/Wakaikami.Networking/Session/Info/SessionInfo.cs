using Wakaikami.Core.Time;

namespace Wakaikami.Networking.Session.Info;

public class SessionInfo
{
    public string RemoteEndPoint { get; set; } = "0.0.0.0:0000";

    public DateTime LastSync { get; set; }

    public DateTime CreateTime { get; set; }

    public TimeSpan TimeFromLastSync => ServerClock.UtcNow - LastSync;

    public TimeSpan OpenTime => ServerClock.UtcNow - CreateTime;

    public string GetIp()
    {
        var portSeparator = RemoteEndPoint.LastIndexOf(':');
        var host = portSeparator < 0 ? RemoteEndPoint : RemoteEndPoint[..portSeparator];
        return host.TrimStart('[').TrimEnd(']');
    }

    public SessionInfo()
    {
        CreateTime = LastSync = ServerClock.UtcNow;
    }
}
