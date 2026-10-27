using LivePulse.Application.Matches;

namespace LivePulse.Application.Abstractions;

public interface IOddsNotifier
{
    Task NotifyAsync(MatchDto match, CancellationToken cancellationToken);
}
