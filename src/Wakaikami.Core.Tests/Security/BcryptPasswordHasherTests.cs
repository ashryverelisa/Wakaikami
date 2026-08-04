using Microsoft.Extensions.Options;
using Wakaikami.Core.Security;

namespace Wakaikami.Core.Tests.Security;

public sealed class BcryptPasswordHasherTests
{
    private const string PepperA = "00112233445566778899aabbccddeeff";
    private const string PepperB = "ffeeddccbbaa99887766554433221100";
    private const string ClientMd5 = "0123456789abcdef0123456789abcdef";

    // Work factor 4 (bcrypt minimum) keeps the tests fast; production uses the validated 10-16 range.
    private static BcryptPasswordHasher Hasher(string pepper) => new(Options.Create(new PasswordHashingOptions { Pepper = pepper, WorkFactor = 4 }));

    [Fact]
    public void VerifyAcceptsTheSameClientMd5()
    {
        var hasher = Hasher(PepperA);

        var hash = hasher.Hash(ClientMd5);

        Assert.True(hasher.Verify(ClientMd5, hash));
    }

    [Fact]
    public void VerifyRejectsADifferentClientMd5()
    {
        var hasher = Hasher(PepperA);

        var hash = hasher.Hash(ClientMd5);

        Assert.False(hasher.Verify("ffffffffffffffffffffffffffffffff", hash));
    }

    [Fact]
    public void VerifyIsCaseInsensitiveOnTheMd5()
    {
        var hasher = Hasher(PepperA);

        var hash = hasher.Hash(ClientMd5.ToUpperInvariant());

        Assert.True(hasher.Verify(ClientMd5.ToLowerInvariant(), hash));
    }

    [Fact]
    public void VerifyFailsWhenPepperDiffers()
    {
        var hash = Hasher(PepperA).Hash(ClientMd5);

        Assert.False(Hasher(PepperB).Verify(ClientMd5, hash));
    }
}
