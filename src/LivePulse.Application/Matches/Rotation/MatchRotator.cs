using LivePulse.Application.Abstractions;
using LivePulse.Domain.Matches;

namespace LivePulse.Application.Matches.Rotation;

public sealed class MatchRotator(
    IMatchRepository matches,
    IUnitOfWork unitOfWork,
    IOddsStore oddsStore,
    IOddsNotifier notifier,
    IClock clock)
{
    public async Task StartNewSeasonAsync(CancellationToken cancellationToken)
    {
        var season = MatchCatalog.CreateSeason();
        matches.AddRange(season);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        foreach (var match in season.Take(MatchRotationRules.ConcurrentLiveMatches))
        {
            await KickOffAsync(match, cancellationToken);
        }
    }

    // Called when a match finishes: the next one in the season takes its place,
    // and when the whole season is over a new one begins.
    public async Task ReplaceFinishedMatchAsync(CancellationToken cancellationToken)
    {
        var next = await matches.GetNextNotStartedAsync(cancellationToken);
        if (next is not null)
        {
            await KickOffAsync(next, cancellationToken);
            return;
        }

        var stillLive = await matches.GetLiveAsync(cancellationToken);
        if (stillLive.Count == 0)
        {
            await StartNewSeasonAsync(cancellationToken);
        }
    }

    private async Task KickOffAsync(Match match, CancellationToken cancellationToken)
    {
        match.Apply(new MatchEvent(match.Id, MatchEventType.Kickoff, null, 0, clock.UtcNow));
        await unitOfWork.SaveChangesAsync(cancellationToken);
        await oddsStore.SetAsync(match.Id, match.Odds, cancellationToken);
        await notifier.NotifyAsync(MatchDto.From(match, match.Odds), cancellationToken);
    }
}
