using System.Diagnostics.Metrics;
using Wakaikami.Database.Login.Repositories.Models;

namespace Wakaikami.Web.Diagnostics;

public static class WebMetrics
{
    public const string MeterName = "Wakaikami.Web";

    private static readonly Meter _meter = new(MeterName);

    private static readonly Counter<long> _registrations = _meter.CreateCounter<long>(
        "wakaikami.web.registrations",
        unit: "{registration}",
        description: "Account registration attempts by result"
    );

    public static void RegistrationCompleted(AccountRegistrationResult result) =>
        _registrations.Add(1, new KeyValuePair<string, object?>("result", result.ToString()));
}
