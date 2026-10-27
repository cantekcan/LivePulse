using LivePulse.Domain.Matches;

namespace LivePulse.Application.Matches;

public sealed record MatchDto(
    Guid Id,
    string HomeTeam,
    string AwayTeam,
    int HomeScore,
    int AwayScore,
    int Minute,
    string Status,
    OddsDto Odds)
{
    public static MatchDto From(Match match, MatchOdds odds) => new(
        match.Id,
        match.HomeTeam,
        match.AwayTeam,
        match.HomeScore,
        match.AwayScore,
        match.Minute,
        match.Status.ToString(),
        OddsDto.From(odds));
}
