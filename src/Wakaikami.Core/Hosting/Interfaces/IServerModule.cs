using Wakaikami.Core.Enums;

namespace Wakaikami.Core.Hosting.Interfaces;

public interface IServerModule : IModule<InitializationStage>
{
    public Task<bool> InitializeAsync();
}
