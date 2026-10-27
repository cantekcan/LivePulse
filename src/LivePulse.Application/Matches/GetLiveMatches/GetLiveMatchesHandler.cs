using LivePulse.Application.Abstractions;
using LivePulse.Domain.Common;

namespace LivePulse.Application.Matches.GetLiveMatches;

public sealed class GetLiveMatchesHandler(IMatchRepository matches, IOddsStore oddsStore)
    : IQueryHandler<GetLiveMatchesQuery, IReadOnlyList<MatchDto>>
{
    public async Task<Result<IReadOnlyList<MatchDto>>> HandleAsync(GetLiveMatchesQuery query, CancellationToken cancellationToken)
    {
        var activeMatches = await matches.GetLiveAsync(cancellationToken);
        var currentOdds = await oddsStore.ListAsync(cancellationToken);

        var dtos = activeMatches
            .Select(match => MatchDto.From(match, currentOdds.GetValueOrDefault(match.Id) ?? match.Odds))
            .ToList();

        return Result<IReadOnlyList<MatchDto>>.Success(dtos);
    }
}
