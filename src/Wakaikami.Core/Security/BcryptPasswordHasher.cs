using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using Wakaikami.Core.Security.Interfaces;

namespace Wakaikami.Core.Security;

/// <summary>
/// Stores passwords as <c>bcrypt( HMAC-SHA256( clientMd5, pepper ) )</c>. The HMAC step folds in a
/// server-side secret so a leaked database cannot be attacked by "shucking" the unsalted client MD5,
/// and bcrypt adds a per-account salt plus an adaptive work factor. The HMAC is passed to bcrypt as a
/// 64-char hex string (well under bcrypt's 72-byte limit and free of NUL bytes that would truncate it).
/// </summary>
public sealed class BcryptPasswordHasher : IPasswordHasher
{
    private readonly byte[] _pepper;
    private readonly int _workFactor;

    public BcryptPasswordHasher(IOptions<PasswordHashingOptions> options)
    {
        var value = options.Value;
        _pepper = Convert.FromHexString(value.Pepper);
        _workFactor = value.WorkFactor;
    }

    public string Hash(string clientMd5) => BCrypt.Net.BCrypt.HashPassword(Peppered(clientMd5), workFactor: _workFactor);

    public bool Verify(string clientMd5, string storedHash) => BCrypt.Net.BCrypt.Verify(Peppered(clientMd5), storedHash);

    private string Peppered(string clientMd5)
    {
        // Normalise casing: the client may send the MD5 in upper or lower case, but the HMAC input
        // must be stable or an account could fail to verify against its own stored hash.
        using var hmac = new HMACSHA256(_pepper);
        var mac = hmac.ComputeHash(Encoding.ASCII.GetBytes(clientMd5.ToLowerInvariant()));
        return Convert.ToHexString(mac);
    }
}
