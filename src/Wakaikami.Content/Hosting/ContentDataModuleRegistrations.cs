using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Wakaikami.Content.Maps;
using Wakaikami.Content.TextFilter.Data;
using Wakaikami.Content.TextFilter.Interfaces;
using Wakaikami.Core.Extensions;
using Wakaikami.Core.Hosting.Enums;
using Wakaikami.Core.Hosting.Interfaces;
using Wakaikami.Core.IO.Shine;
using Wakaikami.Core.IO.Shine.Interfaces;
using Wakaikami.Core.IO.Shn;
using Wakaikami.Core.IO.Shn.Interfaces;

namespace Wakaikami.Content.Hosting;

public class ContentDataModuleRegistrations : IServiceRegistrar
{
    public void Register(IServiceCollection services, InitialType target, IConfiguration configuration)
    {
        services.Configure<ShnOptions>(configuration.GetSection(ShnOptions.SectionName));
        services.AddSingleton<IShnReader, ShnReader>();
        services.AddSingleton<IShineTableReader, ShineTableReader>();

        if (target != InitialType.World)
            return;

        services.AddSingletonAs<MapDataProvider, IDataLoader>();
        services.AddSingletonAs<TextFilterDataProvider, IDataLoader, INameFilter>();
    }
}
