using System.Numerics;
using Wakaikami.Content.Maps.Data.Models.Enums;
using Wakaikami.Core.IO.Shn;

namespace Wakaikami.Content.Maps.Data.Models;

public sealed class MapInfo
{
    [ShnColumn("ID")]
    public ushort Id { get; set; }

    [ShnColumn("MapName")]
    public string Index { get; set; } = string.Empty;

    [ShnColumn("Name")]
    public string Name { get; set; } = string.Empty;

    [ShnColumn("RegenX")]
    public uint RegenX { get; set; }

    [ShnColumn("RegenY")]
    public uint RegenY { get; set; }

    [ShnColumn("KingdomMap")]
    public MapType Type { get; set; }

    [ShnColumn("InSide")]
    public bool InSide { get; set; }

    [ShnColumn("Sight")]
    public double ViewRange { get; set; }

    public Vector2 Regen => new(RegenX, RegenY);
}
