namespace Wakaikami.ZoneService.GameNetwork.Listening.Interfaces;

public interface IZoneSessionManager : IZoneSessionLookup
{
    public int SessionCount { get; }
    public bool TryAcceptConnection(ZoneSession session);
    public bool AddSession(ZoneSession session, ushort sessionId);
    public bool RemoveSession(ZoneSession session);
    public bool GetSessionBySessionId(ushort sessionId, out ZoneSession? session);
}
