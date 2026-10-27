using LivePulse.Domain.Matches;

namespace LivePulse.Application.Matches;

public sealed record OddsDto(decimal Home, decimal Draw, decimal Away)
{
    public static OddsDto From(MatchOdds odds) => new(odds.Home, odds.Draw, odds.Away);
}
