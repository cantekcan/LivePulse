using LivePulse.Domain.Common;

namespace LivePulse.Domain.Matches;

public sealed class Match
{
    private Match(Guid id, string homeTeam, string awayTeam, MatchOdds odds, int seasonOrder)
    {
        Id = id;
        HomeTeam = homeTeam;
        AwayTeam = awayTeam;
        Odds = odds;
        SeasonOrder = seasonOrder;
        Status = MatchStatus.NotStarted;
    }

    // Required by EF Core to materialize the entity.
    private Match()
    {
        HomeTeam = null!;
        AwayTeam = null!;
        Odds = null!;
    }

    public Guid Id { get; private set; }

    public string HomeTeam { get; private set; }

    public string AwayTeam { get; private set; }

    public int SeasonOrder { get; private set; }

    public int HomeScore { get; private set; }

    public int AwayScore { get; private set; }

    public int Minute { get; private set; }

    public MatchStatus Status { get; private set; }

    public MatchOdds Odds { get; private set; }

    public static Match Create(Guid id, string homeTeam, string awayTeam, MatchOdds initialOdds, int seasonOrder = 0) =>
        new(id, homeTeam, awayTeam, initialOdds, seasonOrder);

    public Result Apply(MatchEvent matchEvent)
    {
        var validation = Validate(matchEvent);
        if (validation.IsFailure)
        {
            return validation;
        }

        Minute = matchEvent.Minute;

        switch (matchEvent.Type)
        {
            case MatchEventType.Kickoff:
                Status = MatchStatus.Live;
                break;
            case MatchEventType.Goal:
                ApplyGoal(matchEvent.Side!.Value);
                break;
            case MatchEventType.RedCard:
                ApplyRedCard(matchEvent.Side!.Value);
                break;
            case MatchEventType.FullTime:
                Status = MatchStatus.Finished;
                break;
        }

        return Result.Success();
    }

    private Result Validate(MatchEvent matchEvent)
    {
        if (matchEvent.MatchId != Id)
        {
            return Result.Failure(MatchErrors.WrongMatch);
        }

        if (Status == MatchStatus.Finished)
        {
            return Result.Failure(MatchErrors.AlreadyFinished);
        }

        if (matchEvent.Type == MatchEventType.Kickoff)
        {
            return Status == MatchStatus.NotStarted
                ? Result.Success()
                : Result.Failure(MatchErrors.AlreadyStarted);
        }

        if (Status != MatchStatus.Live)
        {
            return Result.Failure(MatchErrors.NotLive);
        }

        var needsSide = matchEvent.Type is MatchEventType.Goal or MatchEventType.RedCard;
        return needsSide && matchEvent.Side is null
            ? Result.Failure(MatchErrors.SideRequired)
            : Result.Success();
    }

    private void ApplyGoal(MatchSide scorer)
    {
        if (scorer == MatchSide.Home)
        {
            HomeScore++;
            Odds = Odds.Multiply(
                OddsRules.GoalScorerFactor, OddsRules.GoalDrawFactor, OddsRules.GoalConcederFactor);
        }
        else
        {
            AwayScore++;
            Odds = Odds.Multiply(
                OddsRules.GoalConcederFactor, OddsRules.GoalDrawFactor, OddsRules.GoalScorerFactor);
        }
    }

    private void ApplyRedCard(MatchSide offender)
    {
        Odds = offender == MatchSide.Home
            ? Odds.Multiply(OddsRules.RedCardOwnFactor, OddsRules.RedCardDrawFactor, OddsRules.RedCardOpponentFactor)
            : Odds.Multiply(OddsRules.RedCardOpponentFactor, OddsRules.RedCardDrawFactor, OddsRules.RedCardOwnFactor);
    }
}
