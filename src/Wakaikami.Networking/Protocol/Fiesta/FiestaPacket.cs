using Wakaikami.Core.Helpers;
using Wakaikami.Networking.HandlerTypes;

namespace Wakaikami.Networking.Protocol.Fiesta;

public abstract class FiestaPacket : IDisposable
{
    public FiestaHandlerType Header { get; set; }
    public ushort Type { get; set; }
    public bool IsDisposed { get; private set; }

    protected FiestaPacket(FiestaHandlerType header, ushort type)
    {
        Header = header;
        Type = type;
    }

    protected FiestaPacket() { }

    public ushort GetOpcode() => (ushort)(((byte)Header << 10) + (Type & 1023));

    /// <summary>Copy of the packet body for <see cref="ToString"/>/logging; never called on the hot path.</summary>
    protected abstract byte[] BodyBytes();

    public override string ToString() => $"H:{Header}::T{Type} Data : {BodyBytes().BytesToHex()} ";

    public void Dispose()
    {
        if (IsDisposed)
            return;
        IsDisposed = true;
        DisposeInternal();
        GC.SuppressFinalize(this);
    }

    protected virtual void DisposeInternal() { }
}
