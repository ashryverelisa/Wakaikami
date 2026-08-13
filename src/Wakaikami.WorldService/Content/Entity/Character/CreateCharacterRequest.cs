using System.Numerics;

namespace Wakaikami.WorldService.Content.Entity.Character;

public readonly record struct CreateCharacterRequest
{
    public required byte Slot { get; init; }
    public required string Name { get; init; }
    public required byte ClassId { get; init; }
    public required bool IsMale { get; init; }
    public required byte Hair { get; init; }
    public required byte HairColor { get; init; }
    public required byte Face { get; init; }
    public required string MapIndex { get; init; }
    public required Vector2 StartPosition { get; init; }
    public required byte Level { get; init; }
    public required long Money { get; init; }
    public required int Hp { get; init; }
    public required int Sp { get; init; }
    public required int Lp { get; init; }
    public required short HpStones { get; init; }
    public required short SpStones { get; init; }
    public IReadOnlySet<ushort>? Skills { get; init; }
}
