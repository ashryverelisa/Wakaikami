using NSubstitute;
using Wakaikami.Core.Security.Interfaces;
using Wakaikami.Database.Login.Repositories.Interfaces;
using Wakaikami.Database.Login.Repositories.Models;
using Wakaikami.LoginService.Content.Account;
using Wakaikami.LoginService.Content.Account.Models;

namespace Wakaikami.LoginService.Tests.Content.Account;

public sealed class AuthenticationServiceTests
{
    private readonly IAccountRepository _accountRepository = Substitute.For<IAccountRepository>();

    private AuthenticationService CreateAuthenticationService(IPasswordHasher hasher) => new(_accountRepository, hasher);

    private void Returns(AccountLoginRow? row) => _accountRepository.GetLoginInfoAsync("user", Arg.Any<CancellationToken>()).Returns(row);

    private static AccountLoginRow Row(int id = 1, string passwordHash = "$2a$12$stored", bool isActivated = true, bool isBanned = false) =>
        new(id, passwordHash, isActivated, isBanned);

    [Fact]
    public async Task AuthenticateWhenCredentialsValidAndAccountUsableMarksLoggedInAndReturnsSuccess()
    {
        Returns(Row(id: 4242));
        var hasher = new StubPasswordHasher { VerifyResult = true };

        var result = await CreateAuthenticationService(hasher).AuthenticateAsync("user", "md5pw", "127.0.0.1", TestContext.Current.CancellationToken);

        Assert.Equal(new AccountLoginResult(LoginStatus.Success, 4242), result);
        await _accountRepository.Received(1).MarkLoggedInAsync(4242, "127.0.0.1", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AuthenticateWhenPasswordDoesNotVerifyReturnsInvalidAndDoesNotMarkLoggedIn()
    {
        Returns(Row(id: 4242));
        var hasher = new StubPasswordHasher { VerifyResult = false };

        var result = await CreateAuthenticationService(hasher).AuthenticateAsync("user", "wrong", "127.0.0.1", TestContext.Current.CancellationToken);

        Assert.Equal(new AccountLoginResult(LoginStatus.InvalidIdOrPassword, 0), result);
        await _accountRepository.DidNotReceive().MarkLoggedInAsync(Arg.Any<int>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AuthenticateWhenAccountMissingReturnsInvalidWithoutHashing()
    {
        Returns(row: null);
        var hasher = new StubPasswordHasher();

        var result = await CreateAuthenticationService(hasher).AuthenticateAsync("user", "pw", "127.0.0.1", TestContext.Current.CancellationToken);

        Assert.Equal(new AccountLoginResult(LoginStatus.InvalidIdOrPassword, 0), result);
        Assert.Equal(0, hasher.VerifyCallCount);
    }

    [Fact]
    public async Task AuthenticateWhenNotActivatedReturnsAgreementMissingAndDoesNotMarkLoggedIn()
    {
        Returns(Row(isActivated: false));
        var hasher = new StubPasswordHasher { VerifyResult = true };

        var result = await CreateAuthenticationService(hasher).AuthenticateAsync("user", "pw", "127.0.0.1", TestContext.Current.CancellationToken);

        Assert.Equal(new AccountLoginResult(LoginStatus.AgreementMissing, 0), result);
        await _accountRepository.DidNotReceive().MarkLoggedInAsync(Arg.Any<int>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AuthenticateWhenBannedReturnsBlockedAndDoesNotMarkLoggedIn()
    {
        Returns(Row(isBanned: true));
        var hasher = new StubPasswordHasher { VerifyResult = true };

        var result = await CreateAuthenticationService(hasher).AuthenticateAsync("user", "pw", "127.0.0.1", TestContext.Current.CancellationToken);

        Assert.Equal(new AccountLoginResult(LoginStatus.Blocked, 0), result);
        await _accountRepository.DidNotReceive().MarkLoggedInAsync(Arg.Any<int>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AuthenticateVerifiesTheClientMd5AgainstTheStoredHash()
    {
        Returns(Row(id: 9, passwordHash: "$2a$12$stored"));
        var hasher = new StubPasswordHasher { VerifyResult = true };

        await CreateAuthenticationService(hasher).AuthenticateAsync("user", "md5pw", "127.0.0.1", TestContext.Current.CancellationToken);

        Assert.Equal(("md5pw", "$2a$12$stored"), hasher.LastVerify);
    }

    private sealed class StubPasswordHasher : IPasswordHasher
    {
        public bool VerifyResult { get; set; }
        public int VerifyCallCount { get; private set; }
        public (string ClientMd5, string StoredHash) LastVerify { get; private set; }

        public string Hash(string clientMd5) => "$2a$12$stub";

        public bool Verify(string clientMd5, string storedHash)
        {
            VerifyCallCount++;
            LastVerify = (clientMd5, storedHash);
            return VerifyResult;
        }
    }
}
