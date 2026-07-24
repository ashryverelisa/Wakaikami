using System.ComponentModel.DataAnnotations;

namespace Wakaikami.Core.Security;

public sealed class PasswordHashingOptions
{
    public const string SectionName = "PasswordHashing";

    /// <summary>
    /// Server-side secret ("pepper") applied via HMAC-SHA256 before bcrypt, supplied as a hex string
    /// (recommended: 32+ bytes / 64+ hex chars). It MUST come from a secret store user-secrets in
    /// development, an environment variable (<c>PasswordHashing__Pepper</c>) or key vault in
    /// production and must never be committed with a real value. The pepper defeats offline
    /// "password shucking" against the unsalted client MD5; losing or changing it invalidates every
    /// stored hash.
    /// </summary>
    [Required(AllowEmptyStrings = false)]
    [RegularExpression("^[0-9a-fA-F]{32,}$", ErrorMessage = "Pepper must be a hex string of at least 16 bytes (32 hex chars).")]
    public string Pepper { get; set; } = string.Empty;

    /// <summary>bcrypt cost factor. Each increment doubles the work; 12 is a sensible 2020s default.</summary>
    [Range(10, 16)]
    public int WorkFactor { get; set; } = 12;
}
