using LivePulse.Domain.Matches;
using static LivePulse.Domain.Tests.Matches.MatchTestFactory;

namespace LivePulse.Domain.Tests.Matches;

public class MatchTests
{
    [Fact]
    public void Kickoff_MakesMatchLive()
    {
        var match = NotStartedMatch();

        var result = match.Apply(Event(match, MatchEventType.Kickoff, null, minute: 0));

        result.IsSuccess.Should().BeTrue();
        match.Status.Should().Be(MatchStatus.Live);
    }

    [Fact]
    public void HomeGoal_IncreasesScoreAndMovesOddsTowardHome()
    {
        var match = LiveMatch();

        var result = match.Apply(Event(match, MatchEventType.Goal, MatchSide.Home, minute: 23));

        result.IsSuccess.Should().BeTrue();
        match.HomeScore.Should().Be(1);
        match.AwayScore.Should().Be(0);
        match.Minute.Should().Be(23);
        match.Odds.Home.Should().Be(1.60m);
        match.Odds.Draw.Should().Be(3.30m);
        match.Odds.Away.Should().Be(5.00m);
    }

    [Fact]
    public void AwayGoal_IsTheMirrorOfHomeGoal()
    {
        var match = LiveMatch();

        match.Apply(Event(match, MatchEventType.Goal, MatchSide.Away));

        match.AwayScore.Should().Be(1);
        match.Odds.Home.Should().Be(2.50m);
        match.Odds.Draw.Should().Be(3.30m);
        match.Odds.Away.Should().Be(3.20m);
    }

    [Fact]
    public void RedCardForHome_RaisesHomeOddsAndLowersAwayOdds()
    {
        var match = LiveMatch();

        match.Apply(Event(match, MatchEventType.RedCard, MatchSide.Home));

        match.Odds.Home.Should().Be(2.30m);
        match.Odds.Draw.Should().Be(3.00m);
        match.Odds.Away.Should().Be(3.60m);
    }

    [Fact]
    public void RedCardForAway_RaisesAwayOddsAndLowersHomeOdds()
    {
        var match = LiveMatch();

        match.Apply(Event(match, MatchEventType.RedCard, MatchSide.Away));

        match.Odds.Home.Should().Be(1.80m);
        match.Odds.Away.Should().Be(4.60m);
    }

    [Fact]
    public void FullTime_FinishesMatch()
    {
        var match = LiveMatch();

        match.Apply(Event(match, MatchEventType.FullTime, null, minute: 90));

        match.Status.Should().Be(MatchStatus.Finished);
    }

    [Theory]
    [InlineData(MatchEventType.Goal)]
    [InlineData(MatchEventType.RedCard)]
    [InlineData(MatchEventType.Kickoff)]
    [InlineData(MatchEventType.FullTime)]
    public void AnyEventAfterFullTime_IsRejected(MatchEventType type)
    {
        var match = LiveMatch();
        match.Apply(Event(match, MatchEventType.FullTime, null, minute: 90));

        var result = match.Apply(Event(match, type, MatchSide.Home));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(MatchErrors.AlreadyFinished);
    }

    [Fact]
    public void Goal_BeforeKickoff_IsRejected()
    {
        var match = NotStartedMatch();

        var result = match.Apply(Event(match, MatchEventType.Goal, MatchSide.Home));

        result.Error.Should().Be(MatchErrors.NotLive);
        match.HomeScore.Should().Be(0);
    }

    [Fact]
    public void SecondKickoff_IsRejected()
    {
        var match = LiveMatch();

        var result = match.Apply(Event(match, MatchEventType.Kickoff, null));

        result.Error.Should().Be(MatchErrors.AlreadyStarted);
    }

    [Fact]
    public void Goal_WithoutSide_IsRejected()
    {
        var match = LiveMatch();

        var result = match.Apply(Event(match, MatchEventType.Goal, null));

        result.Error.Should().Be(MatchErrors.SideRequired);
    }

    [Fact]
    public void Event_ForAnotherMatch_IsRejected()
    {
        var match = LiveMatch();
        var foreignEvent = new MatchEvent(Guid.NewGuid(), MatchEventType.Goal, MatchSide.Home, 5, DateTimeOffset.UtcNow);

        var result = match.Apply(foreignEvent);

        result.Error.Should().Be(MatchErrors.WrongMatch);
    }

    [Fact]
    public void ManyGoals_NeverPushOddsBelowMinimum()
    {
        var match = LiveMatch();

        for (var i = 0; i < 30; i++)
        {
            match.Apply(Event(match, MatchEventType.Goal, MatchSide.Home));
        }

        match.Odds.Home.Should().BeGreaterThanOrEqualTo(OddsRules.MinimumOdds);
    }
}
