using Grpc.Core;
using Wakaikami.Networking.Grpc.Messages.InternalControl;

namespace Wakaikami.Networking.Grpc;

public sealed class InternalControlService : InternalControl.InternalControlBase
{
    public override Task<PingReply> Ping(PingRequest request, ServerCallContext context) =>
        Task.FromResult(new PingReply { ServerTimeUnixMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() });
}
