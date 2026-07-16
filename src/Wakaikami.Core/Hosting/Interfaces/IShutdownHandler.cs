using Wakaikami.Core.Hosting.Enums;

namespace Wakaikami.Core.Hosting.Interfaces;

public interface IShutdownHandler
{
    public ShutdownOrder Order { get; }
    public void Shutdown();
}
