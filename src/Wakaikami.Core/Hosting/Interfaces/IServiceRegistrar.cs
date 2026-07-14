using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Wakaikami.Core.Enums;

namespace Wakaikami.Core.Hosting.Interfaces;

public interface IServiceRegistrar
{
    public void Register(
        IServiceCollection services,
        InitialType target,
        IConfiguration configuration
    );
}
