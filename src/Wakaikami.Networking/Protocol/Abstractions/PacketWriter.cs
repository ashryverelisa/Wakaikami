using System.Buffers;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using Wakaikami.Core.Helpers;

namespace Wakaikami.Networking.Protocol.Abstractions;

public class PacketWriter : IDisposable
{
    private const int DefaultCapacity = 256;

    private byte[] _buffer = ArrayPool<byte>.Shared.Rent(DefaultCapacity);
    private bool _disposed;

    public int Length { get; private set; }

    public ReadOnlySpan<byte> WrittenSpan => _buffer.AsSpan(0, Length);

    public ReadOnlyMemory<byte> WrittenMemory => _buffer.AsMemory(0, Length);

    protected Encoding Encoding { get; } = Encoding.UTF8;

    public void Write(byte[] data)
    {
        ArgumentNullException.ThrowIfNull(data);
        Write((ReadOnlySpan<byte>)data);
    }

    public void Write(ReadOnlySpan<byte> data)
    {
        EnsureCapacity(data.Length);
        data.CopyTo(_buffer.AsSpan(Length));
        Length += data.Length;
    }

    public void Write<T>(T value)
        where T : unmanaged
    {
        var size = Unsafe.SizeOf<T>();
        EnsureCapacity(size);
        MemoryMarshal.Write(_buffer.AsSpan(Length), in value);
        Length += size;
    }

    public void WriteString(string value, int length)
    {
        ArgumentNullException.ThrowIfNull(value);

        if (length <= 0)
            return;

        EnsureCapacity(length);
        var destination = _buffer.AsSpan(Length, length);
        destination.Clear(); // zero-pad; the encoder only fills the encoded prefix

        // Truncate instead of throwing when the encoded value exceeds the fixed field:
        // player-controlled strings (names, chat) must never kill the send path.
        if (!Encoding.TryGetBytes(value, destination, out _))
        {
            var chars = value.AsSpan();
            while (chars.Length > 0 && !Encoding.TryGetBytes(chars, destination, out _))
                chars = chars[..^1];
        }

        Length += length;
    }

    public void WriteHexAsBytes(string hex) => Write(hex.HexToBytes());

    public void WriteGuid(Guid id) => WriteString(id.ToString("N"), 32);

    public void Fill(byte value, int length)
    {
        if (length <= 0)
            return;

        EnsureCapacity(length);
        _buffer.AsSpan(Length, length).Fill(value);
        Length += length;
    }

    public virtual byte[] ToArray() => WrittenSpan.ToArray();

    public void Dispose()
    {
        if (_disposed)
            return;
        _disposed = true;
        ArrayPool<byte>.Shared.Return(_buffer);
        GC.SuppressFinalize(this);
    }

    private void EnsureCapacity(int additionalBytes)
    {
        var required = Length + additionalBytes;
        if (required <= _buffer.Length)
            return;

        var newBuffer = ArrayPool<byte>.Shared.Rent(Math.Max(_buffer.Length * 2, required));
        _buffer.AsSpan(0, Length).CopyTo(newBuffer);
        ArrayPool<byte>.Shared.Return(_buffer);
        _buffer = newBuffer;
    }
}
