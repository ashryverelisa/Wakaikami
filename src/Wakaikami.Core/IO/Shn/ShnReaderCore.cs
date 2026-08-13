using System.Buffers.Binary;
using Wakaikami.Core.Hashing;
using Wakaikami.Core.IO.Shn.Cryptography;
using Wakaikami.Core.IO.Shn.Enums;

namespace Wakaikami.Core.IO.Shn;

internal static class ShnReaderCore
{
    private const int FileHeaderSize = 32;
    private const int BodyHeaderOverhead = 36;
    private const int ColumnNameLength = 48;

    public static ShnTable Load(string filename)
    {
        if (!File.Exists(filename))
            throw new FileNotFoundException($"Can't find SHN file. {filename}", filename);

        var file = File.ReadAllBytes(filename);

        var totalSize = BinaryPrimitives.ReadInt32LittleEndian(file.AsSpan(FileHeaderSize));
        var bodyLength = totalSize - BodyHeaderOverhead;
        if (bodyLength <= 0 || BodyHeaderOverhead + bodyLength > file.Length)
            bodyLength = file.Length - BodyHeaderOverhead;

        ShnCrypto.Crypt(file.AsSpan(BodyHeaderOverhead, bodyLength));

        var md5Hash = Md5Hash.CalculateMd5Hash(file);
        var reader = new SpanReader(file.AsSpan(BodyHeaderOverhead, bodyLength));

        _ = reader.ReadUInt32(); // header (unused)
        var records = reader.ReadUInt32();
        _ = reader.ReadUInt32();
        var columnCount = reader.ReadUInt32();

        var columns = ReadColumns(ref reader, columnCount);
        var rows = ReadRows(ref reader, columns, records);

        return new ShnTable(columns, rows, md5Hash);
    }

    private static ShnColumn[] ReadColumns(ref SpanReader reader, uint columnCount)
    {
        var columns = new ShnColumn[columnCount];
        var unknown = 0;

        for (var i = 0; i < columnCount; i++)
        {
            var caption = reader.ReadPaddedString(ColumnNameLength).Trim();
            var type = (ShnColumnType)reader.ReadUInt32();
            var length = reader.ReadInt32();

            var name = caption.Length == 0 ? $"Undefined {unknown++}" : caption;
            columns[i] = new ShnColumn(name, type, length, MapColumnType(type));
        }

        return columns;
    }

    private static object[][] ReadRows(ref SpanReader reader, ShnColumn[] columns, uint records)
    {
        var rows = new object[records][];

        for (var r = 0; r < records; r++)
        {
            _ = reader.ReadUInt16();
            var values = new object[columns.Length];

            for (var c = 0; c < columns.Length; c++)
            {
                var col = columns[c];
                values[c] = col.ShnType switch
                {
                    ShnColumnType.CtByte or ShnColumnType.CtInxbyte or ShnColumnType.CtByteBit or ShnColumnType.CtByteArray => reader.ReadByte(),
                    ShnColumnType.CtWord or ShnColumnType.CtInxword or ShnColumnType.CtWordBit => reader.ReadUInt16(),
                    ShnColumnType.CtWordArray => reader.ReadInt16(),

                    ShnColumnType.CtDword
                    or ShnColumnType.CtInx
                    or ShnColumnType.CtInxdword
                    or ShnColumnType.CtDwordBit
                    or ShnColumnType.CtDwordArray
                    or ShnColumnType.CtInxstr => reader.ReadUInt32(),

                    ShnColumnType.CtQword or ShnColumnType.CtInxqword or ShnColumnType.CtQwordBit or ShnColumnType.CtQwordArray => reader.ReadUInt64(),
                    ShnColumnType.CtFloat => reader.ReadSingle(),

                    ShnColumnType.CtFilename
                    or ShnColumnType.CtFileauto
                    or ShnColumnType.CtRemark
                    or ShnColumnType.CtStr
                    or ShnColumnType.CtStrauto
                    or ShnColumnType.CtStrArray
                    or ShnColumnType.CtStrautoArray => reader.ReadPaddedString(col.ShnLength),

                    ShnColumnType.CtVarstr => reader.ReadNullTerminatedString(),
                    ShnColumnType.CtEventUnknown or ShnColumnType.CtUnknowned => reader.ReadHexString(col.ShnLength),
                    ShnColumnType.CtTwoInx => $"{reader.ReadUInt32()}:{reader.ReadUInt32()}",

                    _ => throw new InvalidDataException($"Unknown SHN column type {col.ShnType}"),
                };
            }

            rows[r] = values;
        }

        return rows;
    }

    private static Type MapColumnType(ShnColumnType type) =>
        type switch
        {
            ShnColumnType.CtByte or ShnColumnType.CtInxbyte or ShnColumnType.CtByteBit or ShnColumnType.CtByteArray => typeof(byte),
            ShnColumnType.CtWord or ShnColumnType.CtInxword or ShnColumnType.CtWordBit => typeof(ushort),
            ShnColumnType.CtWordArray => typeof(short),

            ShnColumnType.CtDword
            or ShnColumnType.CtInx
            or ShnColumnType.CtInxdword
            or ShnColumnType.CtDwordBit
            or ShnColumnType.CtDwordArray
            or ShnColumnType.CtInxstr => typeof(uint),

            ShnColumnType.CtQword or ShnColumnType.CtInxqword or ShnColumnType.CtQwordBit or ShnColumnType.CtQwordArray => typeof(ulong),
            ShnColumnType.CtFloat => typeof(float),

            ShnColumnType.CtFilename
            or ShnColumnType.CtFileauto
            or ShnColumnType.CtRemark
            or ShnColumnType.CtStr
            or ShnColumnType.CtStrauto
            or ShnColumnType.CtStrArray
            or ShnColumnType.CtStrautoArray
            or ShnColumnType.CtVarstr
            or ShnColumnType.CtEventUnknown
            or ShnColumnType.CtUnknowned
            or ShnColumnType.CtTwoInx => typeof(string),

            _ => typeof(object),
        };
}
