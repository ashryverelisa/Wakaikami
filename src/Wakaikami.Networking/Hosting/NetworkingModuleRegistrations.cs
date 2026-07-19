using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Wakaikami.Core.Hosting.Enums;
using Wakaikami.Core.Hosting.Interfaces;
using Wakaikami.Networking.HandlerStores;

namespace Wakaikami.Networking.Hosting;

public sealed class NetworkingModuleRegistrations : IServiceRegistrar
{
    public void Register(IServiceCollection services, InitialType target, IConfiguration configuration)
    {
        services.AddSingleton<FiestaHandlerStore>();
        services.AddSingleton<IServerModule>(sp => new FiestaHandlerInitModule(target, sp));
    }

    private sealed class FiestaHandlerInitModule(InitialType target, IServiceProvider services) : IServerModule
    {
        public InitialType InitialType => target;
        public InitializationStage Stage => InitializationStage.PreData;

        public Task<bool> InitializeAsync(CancellationToken cancellationToken) =>
            Task.FromResult(services.GetRequiredService<FiestaHandlerStore>().Initialize());
    }
}
