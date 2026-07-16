using Wakaikami.Core.Hosting.Enums;

namespace Wakaikami.Core.Hosting.Interfaces;

public interface IGameServerModule : IModule<GameInitialStage>
{
    public Task<bool> InitializeAsync(CancellationToken cancellationToken);
}
