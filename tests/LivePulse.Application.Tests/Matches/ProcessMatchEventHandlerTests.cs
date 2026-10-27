using LivePulse.Application.Abstractions;
using LivePulse.Application.Matches;
using LivePulse.Application.Matches.ProcessMatchEvent;
using LivePulse.Application.Matches.Rotation;
using LivePulse.Domain.Matches;

namespace LivePulse.Application.Tests.Matches;

public class ProcessMatchEventHandlerTests
{
    private readonly IMatchRepository _matches = Substitute.For<IMatchRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IOddsStore _oddsStore = Substitute.For<IOddsStore>();
    private readonly IOddsNotifier _notifier = Substitute.For<IOddsNotifier>();
    private readonly Match _match;
    private readonly ProcessMatchEventHandler _handler;

    public ProcessMatchEventHandlerTests()
    {
        _match = Match.Create(Guid.NewGuid(), "Kartal SK", "Aslan FK", MatchOdds.Create(2.00m, 3.00m, 4.00m));
        _match.Apply(new MatchEvent(_match.Id, MatchEventType.Kickoff, null, 0, DateTimeOffset.UtcNow));
        _matches.GetByIdAsync(_match.Id, Arg.Any<CancellationToken>()).Returns(_match);

        var rotator = new MatchRotator(_matches, _unitOfWork, _oddsStore, _notifier, Substitute.For<IClock>());
        _handler = new ProcessMatchEventHandler(_matches, _unitOfWork, _oddsStore, _notifier, rotator);
    }

    [Fact]
    public async Task SavesMatch_WritesOddsToStore_AndNotifiesClients()
    {
        var command = new ProcessMatchEventCommand(_match.Id, MatchEventType.Goal, MatchSide.Home, 12, DateTimeOffset.UtcNow);

        var result = await _handler.HandleAsync(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.HomeScore.Should().Be(1);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        await _oddsStore.Received(1).SetAsync(_match.Id, _match.Odds, Arg.Any<CancellationToken>());
        await _notifier.Received(1).NotifyAsync(Arg.Is<MatchDto>(m => m.Odds.Home == 1.60m), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task FullTime_StartsTheNextMatch()
    {
        var next = Match.Create(Guid.NewGuid(), "Marmara FK", "Ege Gücü", MatchOdds.Create(2.30m, 3.00m, 3.10m), seasonOrder: 4);
        _matches.GetNextNotStartedAsync(Arg.Any<CancellationToken>()).Returns(next);
        var command = new ProcessMatchEventCommand(_match.Id, MatchEventType.FullTime, null, 90, DateTimeOffset.UtcNow);

        var result = await _handler.HandleAsync(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        next.Status.Should().Be(MatchStatus.Live);
        await _notifier.Received(1).NotifyAsync(Arg.Is<MatchDto>(m => m.Id == next.Id && m.Status == "Live"), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ReturnsNotFound_WhenMatchDoesNotExist()
    {
        var command = new ProcessMatchEventCommand(Guid.NewGuid(), MatchEventType.Goal, MatchSide.Home, 12, DateTimeOffset.UtcNow);

        var result = await _handler.HandleAsync(command, CancellationToken.None);

        result.Error.Should().Be(MatchErrors.NotFound);
    }

    [Fact]
    public async Task DoesNotSaveOrNotify_WhenMatchRejectsTheEvent()
    {
        _match.Apply(new MatchEvent(_match.Id, MatchEventType.FullTime, null, 90, DateTimeOffset.UtcNow));
        var command = new ProcessMatchEventCommand(_match.Id, MatchEventType.Goal, MatchSide.Home, 91, DateTimeOffset.UtcNow);

        var result = await _handler.HandleAsync(command, CancellationToken.None);

        result.Error.Should().Be(MatchErrors.AlreadyFinished);
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
        await _notifier.DidNotReceive().NotifyAsync(Arg.Any<MatchDto>(), Arg.Any<CancellationToken>());
    }
}
