using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Wakaikami.Database.Login.Repositories.Interfaces;
using Wakaikami.LoginService.Content.Account;
using Wakaikami.LoginService.Content.Account.Interfaces;
using Wakaikami.LoginService.Content.Account.Models;
using Wakaikami.LoginService.GameNetwork.Protocol.User;

namespace Wakaikami.LoginService.Tests.Content.Account;

public sealed class AccountManagerTests
{
    private readonly IAuthenticationService _authentication = Substitute.For<IAuthenticationService>();
    private readonly IAccountRepository _accountRepository = Substitute.For<IAccountRepository>();

    private AccountManager GetAccountManager() => new(_authentication, _accountRepository, NullLogger<AccountManager>.Instance);

    [Theory]
    [InlineData(LoginStatus.Blocked, UserErrors.LoginBlocked)]
    [InlineData(LoginStatus.AgreementMissing, UserErrors.LoginAgreementMissing)]
    [InlineData(LoginStatus.InvalidIdOrPassword, UserErrors.LoginInvalidIdOrPw)]
    [InlineData(LoginStatus.DatabaseError, UserErrors.LoginDatabaseError)]
    [InlineData(LoginStatus.Failed, UserErrors.LoginFailed)]
    [InlineData(LoginStatus.Success, null)] // success carries no error code
    [InlineData((LoginStatus)0, UserErrors.LoginDatabaseError2)] // empty/absent result set
    public async Task LoginAccountAsyncMapsAuthenticationStatusToUserError(LoginStatus status, UserErrors? expectedError)
    {
        _authentication.AuthenticateAsync("user", "pass", "127.0.0.1", Arg.Any<CancellationToken>()).Returns(new AccountLoginResult(status, 100));

        var (accountId, error) = await GetAccountManager().LoginAccountAsync("user", "pass", "127.0.0.1", TestContext.Current.CancellationToken);

        Assert.Equal(100, accountId);
        Assert.Equal(expectedError, error);
    }

    [Fact]
    public async Task LoginAccountAsyncAuthenticationThrowsReturnsDatabaseError()
    {
        // A failing database must reach the client as a login error, not as an exception escaping the handler.
        _authentication.AuthenticateAsync("user", "pass", "127.0.0.1", Arg.Any<CancellationToken>()).ThrowsAsync(new InvalidOperationException("DB timeout"));

        var (accountId, error) = await GetAccountManager().LoginAccountAsync("user", "pass", "127.0.0.1", TestContext.Current.CancellationToken);

        Assert.Equal(0, accountId);
        Assert.Equal(UserErrors.LoginDatabaseError, error);
    }

    [Fact]
    public async Task LoginAccountAsyncPassesTheCallersCredentialsAndIpThrough()
    {
        _authentication
            .AuthenticateAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new AccountLoginResult(LoginStatus.Success, 1));

        await GetAccountManager().LoginAccountAsync("Hero", "md5hash", "10.0.0.9", TestContext.Current.CancellationToken);

        await _authentication.Received(1).AuthenticateAsync("Hero", "md5hash", "10.0.0.9", Arg.Any<CancellationToken>());
    }
}
