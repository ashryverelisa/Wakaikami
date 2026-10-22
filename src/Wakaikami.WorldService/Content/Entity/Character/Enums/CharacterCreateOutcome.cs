namespace Wakaikami.WorldService.Content.Entity.Character.Enums;

public enum CharacterCreateOutcome
{
    /// <summary>The character row was written; the starter skills may still have failed (logged, not fatal).</summary>
    Created,

    /// <summary>Another character already owns the requested name.</summary>
    NameInUse,

    /// <summary>The account has no free slot left, or the requested slot is taken.</summary>
    MaxSlotReached,

    /// <summary>Anything else: unknown stored-procedure code or a database error.</summary>
    Failed,
}
