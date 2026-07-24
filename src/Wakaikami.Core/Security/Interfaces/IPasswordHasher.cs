namespace Wakaikami.Core.Security.Interfaces;

public interface IPasswordHasher
{
    /// <summary>Produces the value to persist for a fresh registration or a credential change.</summary>
    public string Hash(string clientMd5);

    /// <summary>Verifies a client-supplied MD5 against a previously stored hash.</summary>
    public bool Verify(string clientMd5, string storedHash);
}
