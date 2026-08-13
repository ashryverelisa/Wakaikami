using Wakaikami.WorldService.Content.Entity.Character.Enums;

namespace Wakaikami.WorldService.Content.Entity.Character.Results;

public readonly record struct CreateCharacterResult
{
    /// <summary>Character id reported when no row was created.</summary>
    public const int NoCharacter = -1;

    private CreateCharacterResult(CharacterCreateOutcome outcome, int charId)
    {
        Outcome = outcome;
        CharId = charId;
    }

    public CharacterCreateOutcome Outcome { get; }

    /// <summary>The new character's id, or <see cref="NoCharacter"/> when <see cref="Ok"/> is <c>false</c>.</summary>
    public int CharId { get; }

    public bool Ok => Outcome == CharacterCreateOutcome.Created;

    public static CreateCharacterResult Created(int charId) => new(CharacterCreateOutcome.Created, charId);

    public static CreateCharacterResult Failed(CharacterCreateOutcome outcome) => new(outcome, NoCharacter);
}
