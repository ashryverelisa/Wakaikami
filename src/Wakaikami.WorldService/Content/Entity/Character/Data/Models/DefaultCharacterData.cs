using System.Collections.Frozen;
using System.Numerics;
using Wakaikami.Content.Entity.Character.Data.Models;
using Wakaikami.Content.Maps.Data.Models;
using Wakaikami.Core.IO.Shine;

namespace Wakaikami.WorldService.Content.Entity.Character.Data.Models;

public class DefaultCharacterData
{
    [ShineTableColumn("Class")]
    public byte Class { get; set; }

    [ShineTableColumn("Start Map name")]
    public string StartMapIndex { get; set; } = null!;

    [ShineTableColumn("Start PX")]
    public uint StartX { get; set; }

    [ShineTableColumn("Start PY")]
    public uint StartY { get; set; }

    [ShineTableColumn("HP")]
    public int Hp { get; set; }

    [ShineTableColumn("SP")]
    public int Sp { get; set; }

    [ShineTableColumn("LP")]
    public int Lp { get; set; }

    [ShineTableColumn("HP Soul stone count")]
    public short HpStones { get; set; }

    [ShineTableColumn("SP Soul stone count")]
    public short SpStones { get; set; }

    [ShineTableColumn("Money")]
    public long Money { get; set; }

    [ShineTableColumn("InitLV")]
    public byte StartLevel { get; set; }

    [ShineTableColumn("MinLv")]
    public short MinLevel { get; set; }

    [ShineTableColumn("InitEXP")]
    public ulong StartExp { get; set; }

    public ClassName ClassId { get; set; } = null!;
    public MapInfo StartMap { get; set; } = null!;
    public Vector2 StartPosition => new(StartX, StartY);
    public IReadOnlyDictionary<ushort, byte> Items { get; set; } = FrozenDictionary<ushort, byte>.Empty;
    public IReadOnlySet<ushort> Skills { get; set; } = FrozenSet<ushort>.Empty;
    public IReadOnlyList<DefaultCharacterShortcut> Shortcuts { get; set; } = [];
    public IReadOnlySet<byte> Faces { get; set; } = FrozenSet<byte>.Empty;
    public IReadOnlySet<byte> Hairs { get; set; } = FrozenSet<byte>.Empty;
    public IReadOnlySet<byte> HairColors { get; set; } = FrozenSet<byte>.Empty;
}
