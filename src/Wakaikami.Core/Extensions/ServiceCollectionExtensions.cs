using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;

namespace Wakaikami.Core.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddSingletonAs<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TImpl,
        TService1,
        TService2
    >(this IServiceCollection services)
        where TImpl : class, TService1, TService2
        where TService1 : class
        where TService2 : class
    {
        services.AddSingleton<TImpl>();
        services.AddSingleton<TService1>(sp => sp.GetRequiredService<TImpl>());
        services.AddSingleton<TService2>(sp => sp.GetRequiredService<TImpl>());
        return services;
    }

    public static IServiceCollection AddSingletonAs<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TImpl, TService1>(
        this IServiceCollection services
    )
        where TImpl : class, TService1
        where TService1 : class
    {
        services.AddSingleton<TImpl>();
        services.AddSingleton<TService1>(sp => sp.GetRequiredService<TImpl>());
        return services;
    }

    public static IServiceCollection AddSingletonAs<TImpl, TService1>(this IServiceCollection services, Func<IServiceProvider, TImpl> factory)
        where TImpl : class, TService1
        where TService1 : class
    {
        services.AddSingleton(factory);
        services.AddSingleton<TService1>(sp => sp.GetRequiredService<TImpl>());
        return services;
    }

    public static IServiceCollection AddSingletonAs<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TImpl,
        TService1,
        TService2,
        TService3
    >(this IServiceCollection services)
        where TImpl : class, TService1, TService2, TService3
        where TService1 : class
        where TService2 : class
        where TService3 : class
    {
        services.AddSingleton<TImpl>();
        services.AddSingleton<TService1>(sp => sp.GetRequiredService<TImpl>());
        services.AddSingleton<TService2>(sp => sp.GetRequiredService<TImpl>());
        services.AddSingleton<TService3>(sp => sp.GetRequiredService<TImpl>());
        return services;
    }

    /// <inheritdoc cref="AddSingletonAs{TImpl, TService1}(IServiceCollection, Func{IServiceProvider, TImpl})"/>
    public static IServiceCollection AddSingletonAs<TImpl, TService1, TService2>(this IServiceCollection services, Func<IServiceProvider, TImpl> factory)
        where TImpl : class, TService1, TService2
        where TService1 : class
        where TService2 : class
    {
        services.AddSingleton(factory);
        services.AddSingleton<TService1>(sp => sp.GetRequiredService<TImpl>());
        services.AddSingleton<TService2>(sp => sp.GetRequiredService<TImpl>());
        return services;
    }

    /// <inheritdoc cref="AddSingletonAs{TImpl, TService1, TService2}(IServiceCollection, Func{IServiceProvider, TImpl})"/>
    public static IServiceCollection AddSingletonAs<TImpl, TService1, TService2, TService3>(
        this IServiceCollection services,
        Func<IServiceProvider, TImpl> factory
    )
        where TImpl : class, TService1, TService2, TService3
        where TService1 : class
        where TService2 : class
        where TService3 : class
    {
        services.AddSingleton(factory);
        services.AddSingleton<TService1>(sp => sp.GetRequiredService<TImpl>());
        services.AddSingleton<TService2>(sp => sp.GetRequiredService<TImpl>());
        services.AddSingleton<TService3>(sp => sp.GetRequiredService<TImpl>());
        return services;
    }
}
