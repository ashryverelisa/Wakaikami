namespace Wakaikami.LoginService.Content.Account.Models;

public enum LoginStatus
{
    Blocked = -1,
    AgreementMissing = -2,
    InvalidIdOrPassword = -3,
    DatabaseError = -4,
    Failed = -6,
    Success = 1,
}