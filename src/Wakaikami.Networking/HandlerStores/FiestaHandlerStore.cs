using System.Collections.Frozen;
using Microsoft.Extensions.Logging;
using Wakaikami.Networking.HandlerStores.Interfaces;
using Wakaikami.Networking.HandlerStores.Registration;
using Wakaikami.Networking.HandlerTypes;
using Wakaikami.Networking.Protocol.Fiesta;
using Wakaikami.Networking.Session;

namespace Wakaikami.Networking.HandlerStores;

public sealed partial class FiestaHandlerStore(
    IEnumerable<IGamePacketHandler> handlerServices,
    IEnumerable<GamePacketTypeBinding> typeBindings,
    IServiceProvider services,
    ILogger<FiestaHandlerStore> logger
)
{
    private FrozenDictionary<FiestaHandlerType, FrozenDictionary<ushort, IGamePacketHandler>> _handlers = FrozenDictionary<
        FiestaHandlerType,
        FrozenDictionary<ushort, IGamePacketHandler>
    >.Empty;

    private FrozenDictionary<FiestaHandlerType, FrozenDictionary<ushort, Func<byte[], FiestaClientPacket>>> _factories = FrozenDictionary<
        FiestaHandlerType,
        FrozenDictionary<ushort, Func<byte[], FiestaClientPacket>>
    >.Empty;

    public bool Initialize()
    {
        try
        {
            var handlerCount = 0;
            var typeCount = 0;
            var handlers = new Dictionary<FiestaHandlerType, Dictionary<ushort, IGamePacketHandler>>();
            var factories = new Dictionary<FiestaHandlerType, Dictionary<ushort, Func<byte[], FiestaClientPacket>>>();

            foreach (var h in handlerServices)
            {
                if (!GetInner(handlers, h.HandlerType).TryAdd(h.Opcode, h))
                {
                    LogDuplicateOpcode(h.GetType().Name, h.Opcode, h.HandlerType);
                    continue;
                }

                if (GetInner(factories, h.HandlerType).TryAdd(h.Opcode, h.CreatePacket))
                    typeCount++;
                handlerCount++;
            }

            foreach (var tb in typeBindings)
            {
                if (GetInner(factories, tb.HandlerType).TryAdd(tb.Opcode, tb.Factory))
                    typeCount++;
                else
                    LogDuplicateBinding(tb.Opcode, tb.HandlerType);
            }

            _handlers = handlers.ToFrozenDictionary(kvp => kvp.Key, kvp => kvp.Value.ToFrozenDictionary());
            _factories = factories.ToFrozenDictionary(kvp => kvp.Key, kvp => kvp.Value.ToFrozenDictionary());

            LogLoaded(typeCount, handlerCount);
        }
        catch (Exception ex)
        {
            LogInitializeFailed(ex);
            return false;
        }
        return true;
    }

    public bool TryCreatePacket(FiestaHandlerType header, ushort opcode, byte[] data, out FiestaClientPacket? packet)
    {
        packet = null;
        if (_factories.TryGetValue(header, out var byOpcode) && byOpcode.TryGetValue(opcode, out var factory))
        {
            packet = factory(data);
            return packet is not null;
        }
        return false;
    }

    public async Task HandlePacket(FiestaSession session, FiestaClientPacket packet)
    {
        try
        {
            if (session.IsDisposed)
                return;

            if (_handlers.TryGetValue(packet.Header, out var inner) && inner.TryGetValue(packet.Type, out var handler))
                await handler.Handle(services, session, packet);
            else
                LogUnhandledPacket(packet.ToString());
        }
        catch (Exception ex)
        {
            LogHandlingFailed(ex, packet.GetType().Name, packet);
        }
    }

    private static Dictionary<ushort, TValue> GetInner<TValue>(Dictionary<FiestaHandlerType, Dictionary<ushort, TValue>> outer, FiestaHandlerType handlerType)
    {
        if (!outer.TryGetValue(handlerType, out var inner))
            outer[handlerType] = inner = new Dictionary<ushort, TValue>();
        return inner;
    }

    [LoggerMessage(EventId = 1, Level = LogLevel.Warning, Message = "Unhandled FiestaPacket {Packet}")]
    private partial void LogUnhandledPacket(string? packet);

    [LoggerMessage(EventId = 2, Level = LogLevel.Error, Message = "Error handling {Type} : {Packet}")]
    private partial void LogHandlingFailed(Exception exception, string type, FiestaClientPacket packet);

    [LoggerMessage(EventId = 3, Level = LogLevel.Warning, Message = "Duplicate game handler opcode {Type} H:{HandlerType}::T{Opcode}")]
    private partial void LogDuplicateOpcode(string type, ushort opcode, FiestaHandlerType handlerType);

    [LoggerMessage(EventId = 4, Level = LogLevel.Information, Message = "Loaded {TypeCount} packet types with {HandlerCount} handlers")]
    private partial void LogLoaded(int typeCount, int handlerCount);

    [LoggerMessage(EventId = 5, Level = LogLevel.Error, Message = "Failed to initialize FiestaHandlerStore")]
    private partial void LogInitializeFailed(Exception exception);

    [LoggerMessage(EventId = 6, Level = LogLevel.Warning, Message = "Duplicate packet type binding H:{HandlerType}::T{Opcode} ignored")]
    private partial void LogDuplicateBinding(ushort opcode, FiestaHandlerType handlerType);
}
