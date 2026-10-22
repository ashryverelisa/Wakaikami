using System.Buffers.Binary;
using System.Text;

namespace Wakaikami.Core.IO.Shn;

internal ref struct SpanReader(ReadOnlySpan<byte> data)
{
    private readonly ReadOnlySpan<byte> _data = data;
    private int _pos = 0;

    public byte ReadByte() => _data[_pos++];

    public sbyte ReadSByte() => (sbyte)_data[_pos++];

    public ushort ReadUInt16()
    {
        var v = BinaryPrimitives.ReadUInt16LittleEndian(_data[_pos..]);
        _pos += sizeof(ushort);
        return v;
    }

    public short ReadInt16()
    {
        var v = BinaryPrimitives.ReadInt16LittleEndian(_data[_pos..]);
        _pos += sizeof(short);
        return v;
    }

    public uint ReadUInt32()
    {
        var v = BinaryPrimitives.ReadUInt32LittleEndian(_data[_pos..]);
        _pos += sizeof(uint);
        return v;
    }

    public int ReadInt32()
    {
        var v = BinaryPrimitives.ReadInt32LittleEndian(_data[_pos..]);
        _pos += sizeof(int);
        return v;
    }

    public ulong ReadUInt64()
    {
        var v = BinaryPrimitives.ReadUInt64LittleEndian(_data[_pos..]);
        _pos += sizeof(ulong);
        return v;
    }

    public float ReadSingle()
    {
        var v = BinaryPrimitives.ReadSingleLittleEndian(_data[_pos..]);
        _pos += sizeof(float);
        return v;
    }

    public string ReadPaddedString(int length)
    {
        if (length <= 0)
            return string.Empty;

        var slice = _data.Slice(_pos, length);
        _pos += length;

        var end = slice.IndexOf((byte)0);
        if (end < 0)
            end = length;

        return Encoding.UTF8.GetString(slice[..end]);
    }

    public string ReadNullTerminatedString()
    {
        var remaining = _data[_pos..];
        var end = remaining.IndexOf((byte)0);
        if (end < 0)
        {
            var rest = Encoding.UTF8.GetString(remaining);
            _pos = _data.Length;
            return rest;
        }

        var value = Encoding.UTF8.GetString(remaining[..end]);
        _pos += end + 1;
        return value;
    }

    public string ReadHexString(int length)
    {
        if (length <= 0)
            return string.Empty;

        var slice = _data.Slice(_pos, length);
        _pos += length;
        return Convert.ToHexString(slice);
    }
}