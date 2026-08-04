using System.ComponentModel.DataAnnotations;
using Wakaikami.Core.Security;

namespace Wakaikami.Core.Tests.Security;

public sealed class PasswordHashingOptionsTests
{
    private static List<ValidationResult> Validate(PasswordHashingOptions options)
    {
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(options, new ValidationContext(options), results, validateAllProperties: true);
        return results;
    }

    private static PasswordHashingOptions Valid() => new() { Pepper = new string('a', 64) };

    [Fact]
    public void ValidPepperPasses() => Assert.Empty(Validate(Valid()));

    [Fact]
    public void MissingPepperFailsValidation()
    {
        var options = Valid();
        options.Pepper = string.Empty;

        Assert.Contains(Validate(options), r => r.MemberNames.Contains(nameof(PasswordHashingOptions.Pepper), StringComparer.Ordinal));
    }

    [Theory]
    [InlineData("0011223344556677")] // 16 hex chars = 8 bytes, below the 16-byte floor
    [InlineData("zzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzz")] // right length, not hex
    public void PepperThatIsNotAtLeast16HexBytesFailsValidation(string pepper)
    {
        var options = Valid();
        options.Pepper = pepper;

        Assert.Contains(Validate(options), r => r.MemberNames.Contains(nameof(PasswordHashingOptions.Pepper), StringComparer.Ordinal));
    }
}
