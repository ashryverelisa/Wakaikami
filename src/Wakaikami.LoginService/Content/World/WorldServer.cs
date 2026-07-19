using Microsoft.Extensions.Logging;
using Wakaikami.Content.World.Enums;

namespace Wakaikami.LoginService.Content.World;

public partial class WorldServer(ILogger<WorldServer> logger)
{
    public WorldInfo Info { get; set; } = null!;
    public bool IsReady { get; set; }

    public GameServerState Status
    {
        get => IsReady ? field : GameServerState.Offline;
        private set
        {
            if (Info.IsTestServer && value is GameServerState.Full or GameServerState.Low or GameServerState.Medium or GameServerState.High)
                field = GameServerState.Reserved;
            else
                field = value;
        }
    }

    public void UpdateStatus(GameServerState newStatus)
    {
        LogStatusUpdate(Info.Id, newStatus);
        Status = newStatus;
    }

    [LoggerMessage(EventId = 1, Level = LogLevel.Debug, Message = "Updating WorldServer {WorldId} status to {Status}")]
    private partial void LogStatusUpdate(sbyte worldId, GameServerState status);

    public void RegisterViaGrpc()
    {
        IsReady = true;
        UpdateStatus(GameServerState.Low);
    }

    public void DisconnectGrpc()
    {
        IsReady = false;
    }
}
