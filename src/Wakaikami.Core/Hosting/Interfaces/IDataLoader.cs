using Wakaikami.Core.Hosting.Enums;

namespace Wakaikami.Core.Hosting.Interfaces;

public interface IDataLoader : IModule<DataStage>
{
    public bool Load();
}
