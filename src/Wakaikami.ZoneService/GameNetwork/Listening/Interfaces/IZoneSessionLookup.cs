using Wakaikami.Networking.Session;

namespace Wakaikami.ZoneService.GameNetwork.Listening.Interfaces;

public interface IZoneSessionLookup
{
    public bool TryGetSession(ushort sessionId, out FiestaSession? session);
}
