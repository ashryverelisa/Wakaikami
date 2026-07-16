namespace Wakaikami.Core.Updates.Interfaces;

public interface IUpdatable : IDisposable
{
    public void OnUpdate(DateTime now);

    public bool IsDisposed { get; }

    internal void Tick(DateTime now)
    {
        if (!IsDisposed)
        {
            OnUpdate(now);
        }
    }
}
