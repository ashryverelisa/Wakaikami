using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Options;

namespace Wakaikami.LoginService.Configuration;

public sealed class LoginOptions
{
    public const string SectionName = "Login";

    [Required, ValidateObjectMembers]
    public LoginServerInfoOptions Info { get; set; } = new();
    public string ClientVersion { get; set; } = string.Empty;
}
