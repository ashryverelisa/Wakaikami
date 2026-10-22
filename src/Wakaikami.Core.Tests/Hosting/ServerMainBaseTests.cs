using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Wakaikami.Core.Hosting;
using Wakaikami.Core.Hosting.Enums;
using Wakaikami.Core.Hosting.Interfaces;
using Wakaikami.Core.Time;

namespace Wakaikami.Core.Tests.Hosting;

public sealed class ServerMainBaseTests : IDisposable
{
    private readonly ServiceCollection _serviceCollection = [];
    private readonly ILogger<ServerMainBase> _logger = NullLogger<ServerMainBase>.Instance;
    private readonly ServerTimeProvider _serverTime = new();

    public void Dispose() => ServerClock.Use(TimeProvider.System);

    private ServerMainBase CreateServerMain() => new(InitialType.Login, BuildServices());

    private ServiceProvider BuildServices()
    {
        _serviceCollection.AddSingleton(_logger);
        _serviceCollection.AddSingleton(_serverTime);
        return _serviceCollection.BuildServiceProvider();
    }

    private IServerModule ServerModule(InitialType target = InitialType.Login, InitializationStage stage = InitializationStage.PreData)
    {
        var module = Substitute.For<IServerModule>();
        module.InitialType.Returns(target);
        module.Stage.Returns(stage);
        _serviceCollection.AddSingleton(module);
        return module;
    }

    private IGameServerModule GameServerModule(InitialType target = InitialType.Login)
    {
        var module = Substitute.For<IGameServerModule>();
        module.InitialType.Returns(target);
        _serviceCollection.AddSingleton(module);
        return module;
    }

    private IDataLoader DataLoader(InitialType target = InitialType.Login)
    {
        var loader = Substitute.For<IDataLoader>();
        loader.InitialType.Returns(target);
        _serviceCollection.AddSingleton(loader);
        return loader;
    }

    [Fact]
    public void TickServerClockFollowsProvider()
    {
        CreateServerMain();
        _serverTime.Tick();

        Assert.Equal(_serverTime.GetUtcNow().UtcDateTime, ServerClock.UtcNow);
    }

    [Fact]
    public async Task LoadGameServerModulesAsyncNoModulesReturnsTrueAndSetsFlag()
    {
        var serverMain = CreateServerMain();

        Assert.True(await serverMain.LoadGameServerModulesAsync(TestContext.Current.CancellationToken));
        Assert.True(serverMain.LoadedGameServer);
    }

    [Fact]
    public async Task LoadGameServerModulesAsyncAllSucceedReturnsTrue()
    {
        GameServerModule().InitializeAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult(true));

        Assert.True(await CreateServerMain().LoadGameServerModulesAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task LoadGameServerModulesAsyncModuleReturnsFalseReturnsFalseAndDoesNotSetFlag()
    {
        GameServerModule().InitializeAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult(false));

        var serverMain = CreateServerMain();

        Assert.False(await serverMain.LoadGameServerModulesAsync(TestContext.Current.CancellationToken));
        Assert.False(serverMain.LoadedGameServer);
    }

    [Fact]
    public async Task LoadGameServerModulesAsyncModuleThrowsReturnsFalse()
    {
        GameServerModule().InitializeAsync(Arg.Any<CancellationToken>()).Returns(Task.FromException<bool>(new InvalidOperationException("boom")));

        Assert.False(await CreateServerMain().LoadGameServerModulesAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task LoadGameServerModulesAsyncSkipsOtherInitialTypes()
    {
        var module = GameServerModule(InitialType.Zone);

        Assert.True(await CreateServerMain().LoadGameServerModulesAsync(TestContext.Current.CancellationToken));
        await module.DidNotReceive().InitializeAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task LoadServerModulesAsyncAllSucceedReturnsTrue()
    {
        ServerModule().InitializeAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult(true));

        Assert.True(await CreateServerMain().LoadServerModulesAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task LoadServerModulesAsyncModuleReturnsFalseReturnsFalse()
    {
        ServerModule().InitializeAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult(false));

        Assert.False(await CreateServerMain().LoadServerModulesAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task LoadServerModulesAsyncModuleThrowsReturnsFalse()
    {
        ServerModule().InitializeAsync(Arg.Any<CancellationToken>()).Returns(Task.FromException<bool>(new InvalidOperationException("boom")));

        Assert.False(await CreateServerMain().LoadServerModulesAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task LoadServerModulesAsyncSkipsOtherInitialTypes()
    {
        var module = ServerModule(InitialType.World);

        Assert.True(await CreateServerMain().LoadServerModulesAsync(TestContext.Current.CancellationToken));
        await module.DidNotReceive().InitializeAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task LoadServerModulesAsyncInitializesInStageOrder()
    {
        // Registration order is the reverse of stage order, so only the Stage sort can produce this sequence.
        var calls = new List<string>();
        var networking = ServerModule(stage: InitializationStage.Networking);
        networking.InitializeAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult(true)).AndDoes(_ => calls.Add("networking"));
        var preData = ServerModule(stage: InitializationStage.PreData);
        preData.InitializeAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult(true)).AndDoes(_ => calls.Add("preData"));

        await CreateServerMain().LoadServerModulesAsync(TestContext.Current.CancellationToken);

        Assert.Equal(["preData", "networking"], calls);
    }

    [Fact]
    public void LoadingDataServiceAllLoadersSucceedReturnsTrueAndSetsFlag()
    {
        DataLoader().Load().Returns(true);

        var serverMain = CreateServerMain();

        Assert.True(serverMain.LoadingDataService());
        Assert.True(serverMain.LoadedDataClass);
    }

    [Fact]
    public void LoadingDataServiceLoaderReturnsFalseReturnsFalse()
    {
        DataLoader().Load().Returns(false);

        Assert.False(CreateServerMain().LoadingDataService());
    }

    [Fact]
    public void LoadingDataServiceLoaderThrowsReturnsFalseAndDoesNotSetFlag()
    {
        DataLoader().Load().Returns(_ => throw new InvalidOperationException("boom"));

        var serverMain = CreateServerMain();

        Assert.False(serverMain.LoadingDataService());
        Assert.False(serverMain.LoadedDataClass);
    }

    [Fact]
    public void LoadingDataServiceSkipsOtherInitialTypes()
    {
        var loader = DataLoader(InitialType.Zone);

        Assert.True(CreateServerMain().LoadingDataService());
        loader.DidNotReceive().Load();
    }

    [Fact]
    public void CloseServerCallsAllShutdownHandlers()
    {
        var first = Substitute.For<IShutdownHandler>();
        var second = Substitute.For<IShutdownHandler>();
        _serviceCollection.AddSingleton(first);
        _serviceCollection.AddSingleton(second);

        CreateServerMain().CloseServer();

        first.Received(1).Shutdown();
        second.Received(1).Shutdown();
    }

    [Fact]
    public void CloseServerOneHandlerThrowsOthersStillCalled()
    {
        var bad = Substitute.For<IShutdownHandler>();
        bad.When(x => x.Shutdown()).Throw(new InvalidOperationException("boom"));
        var good = Substitute.For<IShutdownHandler>();
        _serviceCollection.AddSingleton(bad);
        _serviceCollection.AddSingleton(good);

        CreateServerMain().CloseServer();

        good.Received(1).Shutdown();
    }

    [Fact]
    public void CloseServerRunsHandlersInShutdownOrder()
    {
        var calls = new List<ShutdownOrder>();
        var late = Substitute.For<IShutdownHandler>();
        late.Order.Returns(ShutdownOrder.Updates);
        late.When(x => x.Shutdown()).Do(_ => calls.Add(ShutdownOrder.Updates));
        var early = Substitute.For<IShutdownHandler>();
        early.Order.Returns(ShutdownOrder.Network);
        early.When(x => x.Shutdown()).Do(_ => calls.Add(ShutdownOrder.Network));
        _serviceCollection.AddSingleton(late);
        _serviceCollection.AddSingleton(early);

        CreateServerMain().CloseServer();

        Assert.Equal([ShutdownOrder.Network, ShutdownOrder.Updates], calls);
    }
}
