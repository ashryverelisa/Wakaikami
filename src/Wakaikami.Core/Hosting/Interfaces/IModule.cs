using Wakaikami.Core.Enums;

namespace Wakaikami.Core.Hosting.Interfaces;

public interface IModule<out TStage>
{
    public InitialType InitialType { get; }
    public TStage Stage { get; }
}
