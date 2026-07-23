namespace Wakaikami.LoginService.GameNetwork.Protocol.User;

public enum UserErrors
{
    LoginUnkownError = 66,
    LoginDatabaseError = 67,
    LoginInvalidCredentials = 68,
    LoginInvalidIdOrPw = 69,
    LoginDatabaseError2 = 70,
    LoginBlocked = 71,
    LoginServerMaintenance = 72,
    LoginTimeout = 73,
    LoginFailed = 74,
    LoginAgreementMissing = 75,
    LoginWrongRegion = 81,
    ConnectionCut = 1667,
}
