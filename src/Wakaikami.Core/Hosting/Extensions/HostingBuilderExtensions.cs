using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Wakaikami.Core.Hosting.Enums;
using Wakaikami.Core.Hosting.Interfaces;

namespace Wakaikami.Core.Hosting.Extensions;

public static class HostingBuilderExtensions
{
    public static IHostApplicationBuilder AddServiceModules(
        this IHostApplicationBuilder builder,
        InitialType initialType,
        params IServiceRegistrar[] registrars
    )
    {
        builder.Services.AddSingleton(typeof(InitialType), initialType);

        foreach (var r in registrars)
            r.Register(builder.Services, initialType, builder.Configuration);
        return builder;
    }
}
