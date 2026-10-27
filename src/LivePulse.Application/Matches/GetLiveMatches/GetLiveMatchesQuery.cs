using LivePulse.Application.Abstractions;

namespace LivePulse.Application.Matches.GetLiveMatches;

public sealed record GetLiveMatchesQuery : IQuery<IReadOnlyList<MatchDto>>;
