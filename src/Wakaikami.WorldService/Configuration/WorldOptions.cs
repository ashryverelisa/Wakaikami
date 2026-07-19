using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Options;

namespace Wakaikami.WorldService.Configuration;

public sealed class WorldOptions
{
    public const string SectionName = "World";

    [Required, ValidateObjectMembers]
    public WorldInfoOptions WorldInfo { get; set; } = new();

    [Required, ValidateObjectMembers]
    public ZoneInfoOptions ZoneInfo { get; set; } = new();
}
