using Microsoft.Extensions.Logging;
using Wakaikami.Networking.Protocol.Fiesta.Enums;

namespace Wakaikami.WorldService;

internal static partial class LoggerMessages
{
    [LoggerMessage(EventId = 1, Level = LogLevel.Error, Message = "Invalid MaxConnection Count Need more Count as zero..")]
    public static partial void InvalidMaxConnection(this ILogger logger);

    [LoggerMessage(EventId = 2, Level = LogLevel.Error, Message = "WorldServer Port {Port} already in use")]
    public static partial void WorldServerPortInUse(this ILogger logger, int port);

    [LoggerMessage(EventId = 3, Level = LogLevel.Warning, Message = "gRPC RegisterZone rejected: invalid zone id {ZoneId}")]
    public static partial void ZoneRegisterRejected(this ILogger logger, int zoneId);

    [LoggerMessage(EventId = 4, Level = LogLevel.Information, Message = "Zone {ZoneId} registered over gRPC")]
    public static partial void ZoneRegistered(this ILogger logger, int zoneId);

    [LoggerMessage(EventId = 5, Level = LogLevel.Error, Message = "Invalid AwaitingZone Count need more as Zero")]
    public static partial void InvalidAwaitingZoneCount(this ILogger logger);

    [LoggerMessage(EventId = 6, Level = LogLevel.Warning, Message = "Ignored unknown logout type {LogoutType} from {RemoteEndPoint}")]
    public static partial void UnknownLogoutType(this ILogger logger, LogoutType logoutType, string remoteEndPoint);
}
