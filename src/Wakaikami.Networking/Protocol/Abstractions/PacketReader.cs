using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace Wakaikami.Networking.Protocol.Abstractions;

public class PacketReader
{
    private readonly byte[] _buffer;
    private readonly Encoding _encoding;
    private int _position;

    /// <summary>Position captured before the most recent <c>Read*()</c> attempt.</summary>
    public long LastReadPosition { get; private set; }

    /// <summary>
    /// Set by the caller (source-generated or hand-written <c>Read()</c>) immediately before
    /// returning <c>false</c> so the parser can log which field failed.
    /// </summary>
    public string? LastFailedField { get; set; }

    public long Position => _position;

    public long Length => _buffer.Length;

    public long Remaining => _buffer.Length - _position;

    public PacketReader(byte[] buffer)
    {
        ArgumentNullException.ThrowIfNull(buffer);

        _buffer = buffer;
        _encoding = Encoding.UTF8;
    }

    /// <summary>Copy of the raw packet body. Debug/logging only; the hot path never calls this.</summary>
    public byte[] ToArray() => _buffer.ToArray();

    public bool Read<T>(out T value)
        where T : unmanaged
    {
        value = default;
        LastReadPosition = _position;

        var size = Unsafe.SizeOf<T>();
        if (Remaining < size)
            return false;

        ReadOnlySpan<byte> source = _buffer.AsSpan(_position, size);

        // Preserve original semantics: a bool byte must equal 1 to be true.
        if (typeof(T) == typeof(bool))
        {
            var b = source[0] == 1;
            value = Unsafe.As<bool, T>(ref b);
        }
        else
        {
            value = MemoryMarshal.Read<T>(source);
        }

        _position += size;
        return true;
    }

    public bool ReadGuid(out Guid id)
    {
        id = Guid.Empty;
        LastReadPosition = _position;

        if (!ReadString(out var guidString, 32))
            return false;

        return string.IsNullOrEmpty(guidString) || Guid.TryParseExact(guidString, "N", out id);
    }

    public bool ReadString(out string value)
    {
        value = string.Empty;
        LastReadPosition = _position;

        return Read(out byte length) && ReadString(out value, length);
    }

    public bool ReadString(out string value, int length)
    {
        value = string.Empty;
        LastReadPosition = _position;

        if (length <= 0)
            return length == 0;

        if (Remaining < length)
            return false;

        ReadOnlySpan<byte> source = _buffer.AsSpan(_position, length);
        var terminator = source.IndexOf((byte)0);
        var usable = terminator < 0 ? length : terminator;

        value = _encoding.GetString(source[..usable]);
        _position += length;
        return true;
    }

    public bool SkipBytes(int count)
    {
        LastReadPosition = _position;

        if (count < 0 || Remaining < count)
            return false;

        _position += count;
        return true;
    }

    public bool ReadBytes(int length, [NotNullWhen(true)] out byte[]? bytes)
    {
        bytes = null;
        LastReadPosition = _position;

        if (length < 0 || Remaining < length)
            return false;

        bytes = new byte[length];
        _buffer.AsSpan(_position, length).CopyTo(bytes);
        _position += length;
        return true;
    }
}
