using System.Diagnostics.CodeAnalysis;
using System.Security.Cryptography;
using System.Text;
using Wakaikami.Core.Security.Interfaces;
using Wakaikami.Database.Login.Entities;
using Wakaikami.Database.Login.Entities.Enums;
using Wakaikami.Database.Login.Repositories.Interfaces;
using Wakaikami.Database.Login.Repositories.Models;
using Wakaikami.Web.Diagnostics;
using Wakaikami.Web.Services.Interfaces;

namespace Wakaikami.Web.Services;

public sealed class AccountRegistrationService(
    IAccountRegistrationRepository repository,
    IPasswordHasher passwordHasher,
    IHttpContextAccessor httpContextAccessor
) : IAccountRegistrationService
{
    public async Task<bool> IsUsernameAvailableAsync(string username, CancellationToken cancellationToken = default)
    {
        return !await repository.UsernameExistsAsync(username, cancellationToken);
    }

    public async Task<AccountRegistrationResult> RegisterAsync(string username, string password, CancellationToken cancellationToken = default)
    {
        var clientMd5 = ComputeClientMd5(password);

        var account = new Account
        {
            Username = username,
            Password = passwordHasher.Hash(clientMd5),
            CreateAt = DateTime.UtcNow,
            CreationIp = ResolveClientIp(),
            IsActivated = true,
            AccountType = AccountTypes.Player,
        };

        var result = await repository.AddAccountAsync(account, cancellationToken);
        WebMetrics.RegistrationCompleted(result);
        return result;
    }

    [SuppressMessage(
        "Security",
        "CA5351:Do Not Use Broken Cryptographic Algorithms",
        Justification = "MD5 is not used as a security control here. The game client authenticates with MD5(password), "
            + "so the web must reproduce that same client MD5 to stay credential-compatible. The actual at-rest "
            + "protection is bcrypt(HMAC-SHA256(md5, pepper)) applied by IPasswordHasher."
    )]
    private static string ComputeClientMd5(string password)
    {
        var hash = MD5.HashData(Encoding.UTF8.GetBytes(password));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    private string ResolveClientIp()
    {
        var address = httpContextAccessor.HttpContext?.Connection.RemoteIpAddress;
        if (address is null)
            return "0.0.0.0";

        if (address.IsIPv4MappedToIPv6)
            address = address.MapToIPv4();

        var text = address.ToString();

        // CreationIp is capped at 15 chars (IPv4). Fall back rather than overflow on an IPv6 client.
        return text.Length <= 15 ? text : "0.0.0.0";
    }
}
