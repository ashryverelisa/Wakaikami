using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Wakaikami.LoginService.Content.World;
using Wakaikami.LoginService.Tests.TestSupport;

namespace Wakaikami.LoginService.Tests.Content.World;

public sealed class WorldServerManagerTests
{
    private readonly ILogger<WorldServer> _worldLogger = NullLogger<WorldServer>.Instance;

    private WorldServerManager GetWorldServerManager() => new(_worldLogger);

    [Fact]
    public void RegisterNewWorldCreatesServerWithAnnouncedInfo()
    {
        var manager = GetWorldServerManager();
        var info = Fakers.World(id: 1);

        var server = manager.Register(info);

        Assert.Same(info, server.Info);
        Assert.False(server.IsReady);
    }

    [Fact]
    public void RegisterExistingWorldRefreshesInfoAndKeepsServer()
    {
        var manager = GetWorldServerManager();
        var first = manager.Register(Fakers.World(id: 1));
        var updated = Fakers.World(id: 1, isTestServer: true);

        var second = manager.Register(updated);

        Assert.Same(first, second);
        Assert.Same(updated, second.Info);
    }

    [Fact]
    public void RegisterMultipleWorldsToListIsOrderedById()
    {
        var manager = GetWorldServerManager();
        manager.Register(Fakers.World(id: 5));
        manager.Register(Fakers.World(id: 2, isTestServer: true));

        var list = manager.ToList();

        Assert.Equal(2, list.Count);
        Assert.Equal(2, list[0].Info.Id);
        Assert.Equal(5, list[1].Info.Id);
        Assert.True(list[0].Info.IsTestServer);
    }

    [Fact]
    public void GetWorldByIdRegisteredIdReturnsServer()
    {
        var manager = GetWorldServerManager();
        manager.Register(Fakers.World(id: 1));

        var found = manager.GetWorldById(1, out var server);

        Assert.True(found);
        Assert.NotNull(server);
        Assert.Equal(1, server.Info.Id);
    }
}
