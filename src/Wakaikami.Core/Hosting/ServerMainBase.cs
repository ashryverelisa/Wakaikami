using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Wakaikami.Core.Hosting.Enums;
using Wakaikami.Core.Hosting.Interfaces;
using Wakaikami.Core.Time;

namespace Wakaikami.Core.Hosting;

public partial class ServerMainBase : IServerLifecycle
{
    public InitialType InitialType { get; }

    public bool LoadedGameServer
    {
        get => _loadedGameServer;
        private set => _loadedGameServer = value;
    }

    public bool LoadedDataClass
    {
        get => _loadedDataClass;
        private set => _loadedDataClass = value;
    }

    private readonly IServiceProvider _services;
    private readonly Lock _threadLocker;
    private readonly ILogger<ServerMainBase> _logger;
    private volatile bool _loadedDataClass;
    private volatile bool _loadedGameServer;

    public ServerMainBase(InitialType initialType, IServiceProvider services)
    {
        ArgumentNullException.ThrowIfNull(services);

        _services = services;
        _threadLocker = new Lock();
        _logger = services.GetRequiredService<ILogger<ServerMainBase>>();

        ServerClock.Use(services.GetRequiredService<ServerTimeProvider>());

        AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;

        InitialType = initialType;
    }

    private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        // Non-CLS-compliant throws surface as a non-Exception object; log those by value so the
        // common case keeps the structured exception (stack trace as its own field).
        if (e.ExceptionObject is Exception ex)
            LogUnhandledException(ex);
        else
            LogUnhandledExceptionObject(e.ExceptionObject);
    }

    // One-shot startup; runs without the shared lock because module init is now async (await may not
    // run while holding a monitor) and startup does not overlap with LoadingDataService.
    public async Task<bool> LoadGameServerModulesAsync(
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            var modules = GetOrderedModules<IGameServerModule, GameInitialStage>();

            if (
                !await InitializeModulesAsync(
                    modules,
                    m => m.InitializeAsync(cancellationToken),
                    "GameServerModule"
                )
            )
                return false;

            LoadedGameServer = true;
            return true;
        }
        catch (Exception ex)
        {
            LogGameServerStartFailed(ex);
            return false;
        }
    }

    public async Task<bool> LoadServerModulesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var modules = GetOrderedModules<IServerModule, InitializationStage>();

            if (
                !await InitializeModulesAsync(
                    modules,
                    m => m.InitializeAsync(cancellationToken),
                    "ServerModule"
                )
            )
                return false;

            return true;
        }
        catch (Exception ex)
        {
            LogServerStartFailed(ex);
            return false;
        }
    }

    public bool LoadingDataService()
    {
        lock (_threadLocker)
        {
            try
            {
                var loaders = GetOrderedModules<IDataLoader, DataStage>();

                if (!InitializeModulesSync(loaders, l => l.Load(), "DataClass"))
                    return false;

                LoadedDataClass = true;
                return true;
            }
            catch (Exception ex)
            {
                LogDataLoadFailed(ex);
                return false;
            }
        }
    }

    public void CloseServer()
    {
        try
        {
            var shutdowns = _services.GetServices<IShutdownHandler>().OrderBy(h => h.Order);

            foreach (var entry in shutdowns)
            {
                try
                {
                    entry.Shutdown();
                }
                catch (Exception ex)
                {
                    LogShutdownModuleFailed(ex, entry.GetType().Name);
                }
            }
        }
        catch (Exception ex)
        {
            LogShutdownResolveFailed(ex);
        }
    }

    private List<T> GetOrderedModules<T, TStage>()
        where T : IModule<TStage>
    {
        return _services
            .GetServices<T>()
            .Where(m => m.InitialType == InitialType)
            .OrderBy(m => m.Stage)
            .ToList();
    }

    private async Task<bool> InitializeModulesAsync<T>(
        List<T> modules,
        Func<T, Task<bool>> init,
        string moduleTypeName
    )
    {
        foreach (var module in modules)
        {
            try
            {
                if (await init(module))
                    continue;
                LogModuleInitRejected(moduleTypeName, module!.GetType().Name);
                return false;
            }
            catch (Exception ex)
            {
                LogModuleInitFailed(ex, moduleTypeName, module!.GetType().Name);
                return false;
            }
        }

        return true;
    }

    private bool InitializeModulesSync<T>(
        List<T> modules,
        Func<T, bool> init,
        string moduleTypeName
    )
    {
        foreach (var module in modules)
        {
            try
            {
                if (init(module))
                    continue;
                LogModuleInitRejected(moduleTypeName, module!.GetType().Name);
                return false;
            }
            catch (Exception ex)
            {
                LogModuleInitFailed(ex, moduleTypeName, module!.GetType().Name);
                return false;
            }
        }

        return true;
    }

    [LoggerMessage(EventId = 1, Level = LogLevel.Error, Message = "Unhandled exception.")]
    private partial void LogUnhandledException(Exception exception);

    [LoggerMessage(
        EventId = 2,
        Level = LogLevel.Error,
        Message = "Unhandled non-exception object: {ExceptionObject}"
    )]
    private partial void LogUnhandledExceptionObject(object? exceptionObject);

    [LoggerMessage(
        EventId = 3,
        Level = LogLevel.Error,
        Message = "Data loading failed. Errors occurred."
    )]
    private partial void LogDataLoadFailed(Exception exception);

    [LoggerMessage(
        EventId = 4,
        Level = LogLevel.Error,
        Message = "GameServer could not be started. Errors occurred."
    )]
    private partial void LogGameServerStartFailed(Exception exception);

    [LoggerMessage(
        EventId = 5,
        Level = LogLevel.Error,
        Message = "Server could not be started. Errors occurred."
    )]
    private partial void LogServerStartFailed(Exception exception);

    [LoggerMessage(EventId = 6, Level = LogLevel.Error, Message = "Error in CloseServer {Module}")]
    private partial void LogShutdownModuleFailed(Exception exception, string module);

    [LoggerMessage(
        EventId = 7,
        Level = LogLevel.Error,
        Message = "Error resolving shutdown handlers."
    )]
    private partial void LogShutdownResolveFailed(Exception exception);

    [LoggerMessage(
        EventId = 8,
        Level = LogLevel.Error,
        Message = "Invalid Initial {ModuleType} {Module}. Could not start."
    )]
    private partial void LogModuleInitRejected(string moduleType, string module);

    [LoggerMessage(
        EventId = 9,
        Level = LogLevel.Error,
        Message = "Invalid Initial {ModuleType} {Module}"
    )]
    private partial void LogModuleInitFailed(Exception exception, string moduleType, string module);
}
