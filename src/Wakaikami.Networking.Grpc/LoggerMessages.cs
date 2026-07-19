using Microsoft.Extensions.Logging;

namespace Wakaikami.Networking.Grpc;

internal static partial class LoggerMessages
{
    [LoggerMessage(EventId = 1, Level = LogLevel.Information, Message = "Subscribing to {Stream}")]
    public static partial void PushStreamSubscribing(this ILogger logger, string stream);

    [LoggerMessage(EventId = 2, Level = LogLevel.Warning, Message = "{Stream} dropped; reconnecting in {Delay}s")]
    public static partial void PushStreamDropped(this ILogger logger, Exception exception, string stream, double delay);

    [LoggerMessage(EventId = 3, Level = LogLevel.Information, Message = "gRPC {Operation} accepted")]
    public static partial void GrpcRegisterAccepted(this ILogger logger, string operation);

    [LoggerMessage(EventId = 4, Level = LogLevel.Debug, Message = "gRPC {Operation} not yet accepted; retrying")]
    public static partial void GrpcRegisterPending(this ILogger logger, string operation);

    [LoggerMessage(EventId = 5, Level = LogLevel.Debug, Message = "gRPC {Operation} attempt failed; retrying")]
    public static partial void GrpcRegisterAttemptFailed(this ILogger logger, Exception exception, string operation);

    [LoggerMessage(EventId = 6, Level = LogLevel.Debug, Message = "gRPC {Operation} failed; retrying next interval")]
    public static partial void GrpcSyncFailed(this ILogger logger, Exception exception, string operation);

    [LoggerMessage(EventId = 7, Level = LogLevel.Debug, Message = "gRPC fire-and-forget call failed")]
    public static partial void GrpcFireAndForgetFailed(this ILogger logger, Exception exception);
}
