using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace LivePulse.Application.Telemetry;

public static class LivePulseTelemetry
{
    public const string Name = "LivePulse";

    public static readonly ActivitySource ActivitySource = new(Name);

    private static readonly Meter Meter = new(Name);

    public static readonly Counter<long> MatchEventsProcessed =
        Meter.CreateCounter<long>("livepulse.match_events.processed", description: "Match events applied to a match.");

    public static readonly Counter<long> CouponsAccepted =
        Meter.CreateCounter<long>("livepulse.coupons.accepted", description: "Coupons saved successfully.");

    public static readonly Counter<long> CouponsRejectedOddsChanged =
        Meter.CreateCounter<long>("livepulse.coupons.rejected_odds_changed", description: "Coupons rejected because the odds moved.");
}
