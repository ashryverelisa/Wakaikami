using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Wakaikami.Core.Hosting.Enums;
using Wakaikami.Core.Hosting.Interfaces;

namespace Wakaikami.Content.Hosting;

public class ContentDataModuleRegistrations : IServiceRegistrar
{
    public void Register(IServiceCollection services, InitialType target, IConfiguration configuration) { }
}
