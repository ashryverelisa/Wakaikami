using System.Diagnostics.Metrics;
using Wakaikami.LoginService.GameNetwork.Protocol.User;

namespace Wakaikami.LoginService.Diagnostics;

public static class LoginMetrics
{
    public const string MeterName = "Wakaikami.LoginService";

    private static readonly Meter _meter = new(MeterName);

    private static readonly Counter<long> _loginAttempts = _meter.CreateCounter<long>(
        "wakaikami.login.attempts",
        unit: "{attempt}",
        description: "Game client login attempts by result"
    );

    public static void LoginSucceeded() => _loginAttempts.Add(1, new KeyValuePair<string, object?>("result", "success"));

    public static void LoginFailed(UserErrors error) =>
        _loginAttempts.Add(
            1,
            new KeyValuePair<string, object?>("result", "failure"),
            new KeyValuePair<string, object?>("error", error.ToString())
        );
}
