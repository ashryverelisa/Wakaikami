using System.Net;
using Microsoft.Extensions.Logging;

namespace Wakaikami.Networking;

internal static partial class LoggerMessages
{
    // ServerBase<TSession>
    [LoggerMessage(EventId = 1, Level = LogLevel.Information, Message = "Listening on {Endpoint}")]
    public static partial void ServerListening(this ILogger logger, IPEndPoint endpoint);

    [LoggerMessage(EventId = 2, Level = LogLevel.Error, Message = "AcceptAsync failed")]
    public static partial void ServerAcceptFailed(this ILogger logger, Exception exception);

    [LoggerMessage(EventId = 3, Level = LogLevel.Error, Message = "Failed to create or accept session")]
    public static partial void ServerSessionCreateFailed(this ILogger logger, Exception exception);

    [LoggerMessage(EventId = 4, Level = LogLevel.Debug, Message = "Failed to close socket after session creation failure")]
    public static partial void ServerSocketCloseFailed(this ILogger logger, Exception exception);

    // SessionManagerBase<TSession>
    [LoggerMessage(EventId = 5, Level = LogLevel.Error, Message = "Error updating {Manager}")]
    public static partial void SessionManagerUpdateFailed(this ILogger logger, Exception exception, string manager);
}
