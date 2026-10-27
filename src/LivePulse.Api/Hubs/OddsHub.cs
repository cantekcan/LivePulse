using Microsoft.AspNetCore.SignalR;

namespace LivePulse.Api.Hubs;

public sealed class OddsHub : Hub
{
    public const string Path = "/hubs/odds";
    public const string OddsUpdatedMethod = "OddsUpdated";
}
