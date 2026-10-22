using Wakaikami.Content.TextFilter.Interfaces;

namespace Wakaikami.WorldService.Content.Entity.Character;

public sealed class CharacterNameValidator(INameFilter nameFilter)
{
    public bool IsValid(string? name) => !string.IsNullOrWhiteSpace(name) && !nameFilter.IsBadName(name);
}
