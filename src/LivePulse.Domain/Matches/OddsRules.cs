namespace LivePulse.Domain.Matches;

public static class OddsRules
{
    public const decimal MinimumOdds = 1.01m;
    public const int Decimals = 2;

    public const decimal GoalScorerFactor = 0.80m;
    public const decimal GoalDrawFactor = 1.10m;
    public const decimal GoalConcederFactor = 1.25m;

    public const decimal RedCardOwnFactor = 1.15m;
    public const decimal RedCardOpponentFactor = 0.90m;
    public const decimal RedCardDrawFactor = 1.00m;
}
