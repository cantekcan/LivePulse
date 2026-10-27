using LivePulse.Domain.Matches;

namespace LivePulse.Application.Abstractions;

public interface IOddsStore
{
    Task<MatchOdds?> GetAsync(Guid matchId, CancellationToken cancellationToken);

    Task SetAsync(Guid matchId, MatchOdds odds, CancellationToken cancellationToken);

    Task<IReadOnlyDictionary<Guid, MatchOdds>> ListAsync(CancellationToken cancellationToken);
}
