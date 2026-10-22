using System.Numerics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Wakaikami.Networking.Protocol.Fiesta;
using Wakaikami.Networking.Session;

namespace Wakaikami.Content.Entity.Character;

public abstract partial class CharacterBase : IDisposable
{
    private int _disposed;

    public abstract FiestaSession? Session { get; }
    public virtual ushort SessionId { get; set; }
    public bool IsConnected => Session is { IsDisposed: false };
    public bool IsDisposed => Volatile.Read(ref _disposed) != 0;

    /// <summary>Logger of the concrete character; null on the base/un-bound instance.</summary>
    public virtual ILogger? Logger => null;

    /// <summary>Non-null view of <see cref="Logger"/> for the log calls below, so they don't each need a
    /// null-guard. <see cref="NullLogger"/> is disabled for every level, so an un-bound character pays
    /// nothing beyond the IsEnabled check the generated code already does.</summary>
    private ILogger Log => Logger ?? NullLogger.Instance;

    public string Name { get; set; } = string.Empty;
    public int Id { get; set; }
    public ushort Level { get; set; }
    public string MapIndex { get; private set; } = string.Empty;
    public ushort InstanceId { get; private set; }
    public Vector2 Position { get; set; }
    public CharacterShape Shape { get; } = new();

    public void SetMap(string mapIndex, ushort instanceId = 0)
    {
        MapIndex = mapIndex;
        InstanceId = instanceId;
    }

    public void Send(FiestaServerPacket packet, bool destroy = true)
    {
        if (Session is { } session)
        {
            session.SendPacket(packet, destroy);
            return;
        }

        // No session to hand ownership to: dispose here, or the packet's pooled writer buffer
        if (destroy)
            packet.Dispose();
    }

    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0)
            return;

        LogDisposed(Log);

        Session?.Dispose();
        ClearSession();

        GC.SuppressFinalize(this);
    }

    protected virtual void ClearSession() { }

    [LoggerMessage(EventId = 1, Level = LogLevel.Debug, Message = "Character Disposed")]
    private static partial void LogDisposed(ILogger logger);
}
