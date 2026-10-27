using LivePulse.Domain.Coupons;

namespace LivePulse.Domain.Matches;

public sealed record MatchOdds
{
    private MatchOdds(decimal home, decimal draw, decimal away)
    {
        Home = home;
        Draw = draw;
        Away = away;
    }

    public decimal Home { get; private set; }

    public decimal Draw { get; private set; }

    public decimal Away { get; private set; }

    public static MatchOdds Create(decimal home, decimal draw, decimal away) =>
        new(Normalize(home), Normalize(draw), Normalize(away));

    public MatchOdds Multiply(decimal homeFactor, decimal drawFactor, decimal awayFactor) =>
        Create(Home * homeFactor, Draw * drawFactor, Away * awayFactor);

    public decimal For(CouponPick pick) => pick switch
    {
        CouponPick.Home => Home,
        CouponPick.Draw => Draw,
        _ => Away
    };

    private static decimal Normalize(decimal value)
    {
        var rounded = Math.Round(value, OddsRules.Decimals, MidpointRounding.AwayFromZero);
        return Math.Max(rounded, OddsRules.MinimumOdds);
    }
}
