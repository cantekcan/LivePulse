using LivePulse.Application.Abstractions;
using LivePulse.Application.Matches;
using LivePulse.Application.Telemetry;
using Microsoft.AspNetCore.SignalR;

namespace LivePulse.Api.Hubs;

public sealed class SignalROddsNotifier(IHubContext<OddsHub> hub) : IOddsNotifier
{
    public async Task NotifyAsync(MatchDto match, CancellationToken cancellationToken)
    {
        using var activity = LivePulseTelemetry.ActivitySource.StartActivity("signalr broadcast OddsUpdated");
        activity?.SetTag("livepulse.match_id", match.Id);

        await hub.Clients.All.SendAsync(OddsHub.OddsUpdatedMethod, match, cancellationToken);
    }
}
