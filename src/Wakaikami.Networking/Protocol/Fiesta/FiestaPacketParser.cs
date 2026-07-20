using System.Buffers;
using System.Buffers.Binary;
using Microsoft.Extensions.Logging;
using Wakaikami.Core.Helpers;
using Wakaikami.Networking.HandlerStores;
using Wakaikami.Networking.HandlerTypes;
using Wakaikami.Networking.Protocol.Abstractions.Interfaces;
using Wakaikami.Networking.Session;

namespace Wakaikami.Networking.Protocol.Fiesta;

public sealed partial class FiestaPacketParser(FiestaSession session, ILogger logger, FiestaHandlerStore fiestaStore) : IPacketParser
{
    private readonly List<PendingPacket> _frames = new();

    private readonly record struct PendingPacket(ushort Opcode, byte[] Body);

    public async ValueTask<ParseResult> ParseAsync(ReadOnlySequence<byte> buffer, CancellationToken cancellationToken)
    {
        var consumed = ExtractFrames(buffer, out var malformed);

        try
        {
            foreach (var frame in _frames)
            {
                try
                {
                    await HandlePacketAsync(frame.Opcode, frame.Body);
                }
                catch (Exception ex)
                {
                    LogHandleDataFailed(ex);
                }
            }
        }
        finally
        {
            _frames.Clear();
        }

        if (malformed)
            session.Dispose();

        return new ParseResult(consumed, buffer.End);
    }

    private SequencePosition ExtractFrames(in ReadOnlySequence<byte> buffer, out bool malformed)
    {
        var reader = new SequenceReader<byte>(buffer);
        malformed = false;

        while (TryReadFrame(ref reader, ref malformed)) { }

        return reader.Position;
    }

    private bool TryReadFrame(ref SequenceReader<byte> reader, ref bool malformed)
    {
        if (!reader.TryPeek(out var firstByte))
            return false;

        int headerSize;
        int frameSize;

        if (firstByte != 0)
        {
            headerSize = 1;
            frameSize = firstByte;
        }
        else
        {
            if (reader.Remaining < 3)
                return false;

            Span<byte> headerSpan = stackalloc byte[3];
            reader.TryCopyTo(headerSpan);
            headerSize = 3;
            frameSize = BinaryPrimitives.ReadUInt16LittleEndian(headerSpan[1..]);
        }

        if (reader.Remaining < headerSize + frameSize)
            return false;

        if (frameSize < 2)
        {
            malformed = true;
            return false;
        }

        reader.Advance(headerSize);

        Span<byte> opcodeBytes = stackalloc byte[2];
        reader.TryCopyTo(opcodeBytes);
        reader.Advance(2);
        session.Crypto.Crypt(opcodeBytes);
        var opcode = BinaryPrimitives.ReadUInt16LittleEndian(opcodeBytes);

        var body = new byte[frameSize - 2];
        reader.TryCopyTo(body);
        reader.Advance(body.Length);
        session.Crypto.Crypt(body);

        _frames.Add(new PendingPacket(opcode, body));
        return true;
    }

    private async Task HandlePacketAsync(ushort opcode, byte[] body)
    {
        var header = (FiestaHandlerType)(opcode >> 10);
        var type = (ushort)(opcode & 1023);

        if (!fiestaStore.TryCreatePacket(header, type, body, out var packet))
        {
            LogPacketCreateFailed(header, ((int)header).ToString(System.Globalization.CultureInfo.InvariantCulture), type, body.BytesToHex());
            return;
        }

        packet!.Header = header;
        packet.Type = type;

        if (!packet.Read())
        {
            var error = packet.GetReadError();
            LogPacketReadFailed(
                packet.GetType().Name,
                error.Position,
                error.Remaining,
                error.Field is not null ? $", field '{error.Field}'" : "",
                body.BytesToHex()
            );
            session.Dispose();
            return;
        }

        await fiestaStore.HandlePacket(session, packet);
    }

    [LoggerMessage(EventId = 1, Level = LogLevel.Error, Message = "FiestaPacketParser HandleData error")]
    private partial void LogHandleDataFailed(Exception exception);

    [LoggerMessage(EventId = 2, Level = LogLevel.Warning, Message = "Failed to Get FiestaPacket H:{Header} ({HeaderDecimal}) T{Type} Data: {Data}")]
    private partial void LogPacketCreateFailed(FiestaHandlerType header, string headerDecimal, ushort type, string data);

    [LoggerMessage(
        EventId = 3,
        Level = LogLevel.Warning,
        Message = "Failed to Read FiestaPacket {PacketType} at offset {Offset} ({Remaining} bytes left){Field}. Data: {Data}"
    )]
    private partial void LogPacketReadFailed(string packetType, long offset, long remaining, string field, string data);
}
