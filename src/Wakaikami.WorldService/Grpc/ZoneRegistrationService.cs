using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Microsoft.Extensions.Logging;
using Wakaikami.Networking.Grpc.Messages.ZoneRegistration;
using Wakaikami.WorldService.Content.Zone;

namespace Wakaikami.WorldService.Grpc;

public sealed class ZoneRegistrationService(ZoneManager zones, ILogger<ZoneRegistrationService> logger) : ZoneRegistration.ZoneRegistrationBase
{
    public override Task<RegisterZoneReply> RegisterZone(RegisterZoneRequest request, ServerCallContext context)
    {
        if (!zones.GetZoneById((byte)request.ZoneId, out var zone, onlyConnect: false))
        {
            logger.ZoneRegisterRejected(request.ZoneId);
            return Task.FromResult(new RegisterZoneReply { Accepted = false });
        }

        zone.RegisterViaGrpc(
            new ZoneConnectInfo
            {
                ConnectIp = request.ConnectIp,
                ConnectPort = request.ConnectPort,
                MaxPlayer = (ushort)request.MaxPlayer,
                IsTestServer = request.IsTestServer,
                NowPlayer = 0,
            }
        );

        logger.ZoneRegistered(request.ZoneId);

        return Task.FromResult(new RegisterZoneReply { Accepted = true });
    }

    public override Task<Empty> ZoneSync(ZoneSyncRequest request, ServerCallContext context)
    {
        if (zones.GetZoneById((byte)request.ZoneId, out var zone, onlyConnect: false))
            zone.Update(request.NowPlayers);

        return Task.FromResult(new Empty());
    }
}
