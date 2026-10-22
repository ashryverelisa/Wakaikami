using Microsoft.Extensions.Logging;

namespace Wakaikami.Content;

internal static partial class LoggerMessages
{
    [LoggerMessage(EventId = 1, Level = LogLevel.Error, Message = "Failed Load {What}")]
    public static partial void ShnLoadFailed(this ILogger logger, Exception exception, string what);
}
