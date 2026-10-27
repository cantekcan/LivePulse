using LivePulse.Domain.Matches;

namespace LivePulse.Domain.Tests.Matches;

internal static class MatchTestFactory
{
    public static Match NotStartedMatch() =>
        Match.Create(Guid.NewGuid(), "Kartal SK", "Aslan FK", MatchOdds.Create(2.00m, 3.00m, 4.00m));

    public static Match LiveMatch()
    {
        var match = NotStartedMatch();
        match.Apply(Event(match, MatchEventType.Kickoff, null)).IsSuccess.Should().BeTrue();
        return match;
    }

    public static MatchEvent Event(Match match, MatchEventType type, MatchSide? side, int minute = 10) =>
        new(match.Id, type, side, minute, DateTimeOffset.UtcNow);
}
