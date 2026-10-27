using LivePulse.Application.Abstractions;
using LivePulse.Application.Matches;
using LivePulse.Application.Matches.Rotation;
using LivePulse.Domain.Matches;

namespace LivePulse.Application.Tests.Matches;

public class MatchRotatorTests
{
    private readonly IMatchRepository _matches = Substitute.For<IMatchRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IOddsStore _oddsStore = Substitute.For<IOddsStore>();
    private readonly IOddsNotifier _notifier = Substitute.For<IOddsNotifier>();
    private readonly MatchRotator _rotator;

    public MatchRotatorTests()
    {
        _matches.GetLiveAsync(Arg.Any<CancellationToken>()).Returns([]);
        _rotator = new MatchRotator(_matches, _unitOfWork, _oddsStore, _notifier, Substitute.For<IClock>());
    }

    [Fact]
    public async Task NewSeason_CreatesTwelveMatches_AndStartsTheFirstThree()
    {
        IReadOnlyList<Match> season = [];
        _matches.When(m => m.AddRange(Arg.Any<IEnumerable<Match>>()))
            .Do(call => season = call.Arg<IEnumerable<Match>>().ToList());

        await _rotator.StartNewSeasonAsync(CancellationToken.None);

        season.Should().HaveCount(12);
        season.Where(m => m.Status == MatchStatus.Live).Select(m => m.SeasonOrder).Should().BeEquivalentTo([1, 2, 3]);
        season.Where(m => m.Status == MatchStatus.NotStarted).Should().HaveCount(9);
        await _notifier.Received(3).NotifyAsync(Arg.Any<MatchDto>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ReplaceFinishedMatch_StartsTheNextMatchInTheSeason()
    {
        var next = Match.Create(Guid.NewGuid(), "Marmara FK", "Ege Gücü", MatchOdds.Create(2.30m, 3.00m, 3.10m), seasonOrder: 4);
        _matches.GetNextNotStartedAsync(Arg.Any<CancellationToken>()).Returns(next);

        await _rotator.ReplaceFinishedMatchAsync(CancellationToken.None);

        next.Status.Should().Be(MatchStatus.Live);
        await _oddsStore.Received(1).SetAsync(next.Id, next.Odds, Arg.Any<CancellationToken>());
        _matches.DidNotReceive().AddRange(Arg.Any<IEnumerable<Match>>());
    }

    [Fact]
    public async Task ReplaceFinishedMatch_DoesNothing_WhenOtherMatchesAreStillLive()
    {
        var live = Match.Create(Guid.NewGuid(), "Marmara FK", "Ege Gücü", MatchOdds.Create(2.30m, 3.00m, 3.10m), seasonOrder: 12);
        live.Apply(new MatchEvent(live.Id, MatchEventType.Kickoff, null, 0, DateTimeOffset.UtcNow));
        _matches.GetLiveAsync(Arg.Any<CancellationToken>()).Returns([live]);

        await _rotator.ReplaceFinishedMatchAsync(CancellationToken.None);

        _matches.DidNotReceive().AddRange(Arg.Any<IEnumerable<Match>>());
    }

    [Fact]
    public async Task ReplaceFinishedMatch_StartsNewSeason_WhenEverythingIsFinished()
    {
        await _rotator.ReplaceFinishedMatchAsync(CancellationToken.None);

        _matches.Received(1).AddRange(Arg.Is<IEnumerable<Match>>(season => season.Count() == 12));
    }
}
