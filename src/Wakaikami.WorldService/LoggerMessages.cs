using Microsoft.Extensions.Logging;

namespace Wakaikami.WorldService;

internal static partial class LoggerMessages
{
    [LoggerMessage(EventId = 1, Level = LogLevel.Error, Message = "Invalid MaxConnection Count Need more Count as zero..")]
    public static partial void InvalidMaxConnection(this ILogger logger);

    [LoggerMessage(EventId = 2, Level = LogLevel.Error, Message = "WorldServer Port {Port} already in use")]
    public static partial void WorldServerPortInUse(this ILogger logger, int port);
}
