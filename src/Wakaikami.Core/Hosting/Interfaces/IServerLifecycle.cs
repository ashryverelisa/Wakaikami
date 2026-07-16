namespace Wakaikami.Core.Hosting.Interfaces;

public interface IServerLifecycle
{
    public bool LoadedGameServer { get; }
    public bool LoadedDataClass { get; }

    public Task<bool> LoadGameServerModulesAsync(CancellationToken cancellationToken = default);
    public Task<bool> LoadServerModulesAsync(CancellationToken cancellationToken = default);
    public bool LoadingDataService();
    public void CloseServer();
}
