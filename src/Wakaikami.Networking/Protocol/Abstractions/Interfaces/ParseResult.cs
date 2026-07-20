namespace Wakaikami.Networking.Protocol.Abstractions.Interfaces;

public readonly record struct ParseResult(SequencePosition Consumed, SequencePosition Examined);