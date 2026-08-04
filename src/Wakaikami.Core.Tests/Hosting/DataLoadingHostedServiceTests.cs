using NSubstitute;
using Wakaikami.Core.Hosting;
using Wakaikami.Core.Hosting.Interfaces;

namespace Wakaikami.Core.Tests.Hosting;

public sealed class DataLoadingHostedServiceTests
{
    private readonly IServerLifecycle _lifecycle = Substitute.For<IServerLifecycle>();

    private DataLoadingHostedService CreateService() => new(_lifecycle);

    [Fact]
    public async Task StartAsyncLoadingDataFailsThrowsToAbortHostStartup()
    {
        _lifecycle.LoadingDataService().Returns(false);
        var service = CreateService();

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.StartAsync(TestContext.Current.CancellationToken));
    }
}
