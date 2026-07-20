using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Testing.Platform.Logging;
using NSubstitute;
using Wakaikami.Networking.HandlerStores;
using Wakaikami.Networking.HandlerStores.Interfaces;
using Wakaikami.Networking.HandlerStores.Registration;
using Wakaikami.Networking.HandlerTypes;
using Wakaikami.Networking.Protocol.Fiesta;
using Xunit;

namespace Wakaikami.Networking.Tests;

[Collection("PacketStream")]
public sealed class HandlerStoreTests
{
    private static byte[] EmptyPacketData => [0x00];

    private static FiestaHandlerStore NewStore(IEnumerable<IGamePacketHandler>? handlers = null, IEnumerable<GamePacketTypeBinding>? bindings = null)
    {
        return new FiestaHandlerStore(handlers ?? [], bindings ?? [], Substitute.For<IServiceProvider>(), NullLogger<FiestaHandlerStore>.Instance);
    }

    private static GamePacketTypeBinding Binding(ushort opcode, Func<byte[], FiestaClientPacket> factory) => new(FiestaHandlerType.User, opcode, factory);

    [Fact]
    public void TryCreatePacketRegisteredBindingReturnsPacket()
    {
        var store = NewStore(bindings: [Binding(42, _ => new TestGamePacket())]);
        Assert.True(store.Initialize());

        var ok = store.TryCreatePacket(FiestaHandlerType.User, 42, EmptyPacketData, out var packet);

        Assert.True(ok);
        Assert.NotNull(packet);
    }

    [Fact]
    public void TryCreatePacketUnknownOpcodeReturnsFalse()
    {
        var store = NewStore();
        Assert.True(store.Initialize());

        var ok = store.TryCreatePacket(FiestaHandlerType.User, 999, EmptyPacketData, out var packet);

        Assert.False(ok);
        Assert.Null(packet);
    }

    [Fact]
    public void TryCreatePacketBeforeInitializeReturnsFalse()
    {
        var store = NewStore(bindings: [Binding(42, _ => new TestGamePacket())]);

        var ok = store.TryCreatePacket(FiestaHandlerType.User, 42, EmptyPacketData, out _);

        Assert.False(ok);
    }

    [Fact]
    public void TryCreatePacketFactoryReturnsNullReturnsFalse()
    {
        var store = NewStore(bindings: [Binding(55, static _ => null!)]);
        Assert.True(store.Initialize());

        var ok = store.TryCreatePacket(FiestaHandlerType.User, 55, EmptyPacketData, out _);

        Assert.False(ok);
    }

    [Fact]
    public void InitializeDuplicateBindingKeepsFirstFactory()
    {
        var first = new TestGamePacket();
        var second = new TestGamePacket();
        var store = NewStore(bindings: [Binding(7, _ => first), Binding(7, _ => second)]);

        Assert.True(store.Initialize());

        store.TryCreatePacket(FiestaHandlerType.User, 7, EmptyPacketData, out var packet);
        Assert.Same(first, packet);
    }

    [Fact]
    public void InitializeNoHandlersReturnsTrue()
    {
        var store = NewStore();

        Assert.True(store.Initialize());
    }

    [Fact]
    public void InitializeHandlerRegistersItsFactory()
    {
        var created = new TestGamePacket();
        var handler = Substitute.For<IGamePacketHandler>();
        handler.HandlerType.Returns(FiestaHandlerType.User);
        handler.Opcode.Returns((ushort)11);
        handler.CreatePacket(Arg.Any<byte[]>()).Returns(created);

        var store = NewStore(handlers: [handler]);
        Assert.True(store.Initialize());

        var ok = store.TryCreatePacket(FiestaHandlerType.User, 11, EmptyPacketData, out var packet);

        Assert.True(ok);
        Assert.Same(created, packet);
    }

    /// <summary>Minimal concrete packet for handler store testing.</summary>
    private sealed class TestGamePacket() : FiestaClientPacket(Array.Empty<byte>())
    {
        public override bool Read() => true;
    }
}
