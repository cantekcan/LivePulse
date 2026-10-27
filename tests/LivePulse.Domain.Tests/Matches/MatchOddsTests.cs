using LivePulse.Domain.Matches;

namespace LivePulse.Domain.Tests.Matches;

public class MatchOddsTests
{
    [Fact]
    public void Create_RaisesOddsBelowMinimumToMinimum()
    {
        var odds = MatchOdds.Create(1.00m, 0.50m, 1.01m);

        odds.Home.Should().Be(1.01m);
        odds.Draw.Should().Be(1.01m);
        odds.Away.Should().Be(1.01m);
    }

    [Fact]
    public void Create_RoundsToTwoDecimals()
    {
        var odds = MatchOdds.Create(2.345m, 3.001m, 4.994m);

        odds.Home.Should().Be(2.35m);
        odds.Draw.Should().Be(3.00m);
        odds.Away.Should().Be(4.99m);
    }
}
