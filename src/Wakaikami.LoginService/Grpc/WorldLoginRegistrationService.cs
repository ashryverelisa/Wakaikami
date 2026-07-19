using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Microsoft.Extensions.Logging;
using Wakaikami.Content.World.Enums;
using Wakaikami.LoginService.Content.World;
using Wakaikami.LoginService.Content.World.Interfaces;
using Wakaikami.Networking.Grpc.Messages.WorldLoginRegistration;

namespace Wakaikami.LoginService.Grpc;

public sealed partial class WorldLoginRegistrationService(IWorldServerManager worldServers, ILogger<WorldLoginRegistrationService> logger)
    : WorldLoginRegistration.WorldLoginRegistrationBase
{
    public override Task<RegisterWorldReply> RegisterWorld(RegisterWorldRequest request, ServerCallContext context)
    {
        if (!TryValidate(request, out var info))
        {
            LogRegisterRejected(request.WorldId, request.WorldName, request.ConnectIp, request.ConnectPort);
            return Task.FromResult(new RegisterWorldReply { Accepted = false });
        }

        // Another live registration still holds this id (e.g. the previous stream hasn't dropped yet,
        // or a second instance is misconfigured). Reject; the world retries until the id frees up.
        if (worldServers.GetWorldById(info.Id, out var existing) && existing.IsReady)
        {
            LogRegisterConflict(request.WorldId);
            return Task.FromResult(new RegisterWorldReply { Accepted = false });
        }

        var server = worldServers.Register(info);
        server.RegisterViaGrpc();

        LogWorldRegistered(request.WorldId, info.WorldName, info.Ip, info.Port);

        return Task.FromResult(new RegisterWorldReply { Accepted = true });
    }

    public override Task<Empty> WorldSync(WorldSyncRequest request, ServerCallContext context)
    {
        if (worldServers.GetWorldById((sbyte)request.WorldId, out var server))
            server.UpdateStatus((GameServerState)request.State);

        return Task.FromResult(new Empty());
    }

    private static bool TryValidate(RegisterWorldRequest request, [System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out WorldInfo? info)
    {
        info = null;

        if (request.WorldId is <= 0 or > sbyte.MaxValue)
            return false;
        if (string.IsNullOrWhiteSpace(request.WorldName) || request.WorldName.Length > 16)
            return false;
        if (string.IsNullOrWhiteSpace(request.ConnectIp) || request.ConnectIp.Length > 15)
            return false;
        if (request.ConnectPort is <= 0 or > ushort.MaxValue)
            return false;

        info = new WorldInfo
        {
            Id = (sbyte)request.WorldId,
            WorldName = request.WorldName,
            Ip = request.ConnectIp,
            Port = (ushort)request.ConnectPort,
            IsTestServer = request.IsTestServer,
        };
        return true;
    }

    [LoggerMessage(
        EventId = 1,
        Level = LogLevel.Warning,
        Message = "gRPC RegisterWorld rejected: invalid parameters (id {WorldId}, name '{WorldName}', {ConnectIp}:{ConnectPort})"
    )]
    private partial void LogRegisterRejected(int worldId, string worldName, string connectIp, int connectPort);

    [LoggerMessage(EventId = 2, Level = LogLevel.Information, Message = "World {WorldId} '{WorldName}' registered over gRPC ({Ip}:{Port})")]
    private partial void LogWorldRegistered(int worldId, string worldName, string ip, int port);

    [LoggerMessage(EventId = 3, Level = LogLevel.Warning, Message = "gRPC RegisterWorld rejected: world {WorldId} is already registered and connected")]
    private partial void LogRegisterConflict(int worldId);
}
