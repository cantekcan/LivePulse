using LivePulse.Application.Abstractions;
using LivePulse.Application.Coupons.PlaceCoupon;
using LivePulse.Domain.Coupons;
using LivePulse.Domain.Matches;

namespace LivePulse.Application.Tests.Coupons;

public class PlaceCouponHandlerTests
{
    private const string Key = "key-1";

    private readonly ICouponRepository _coupons = Substitute.For<ICouponRepository>();
    private readonly IMatchRepository _matches = Substitute.For<IMatchRepository>();
    private readonly IOddsStore _oddsStore = Substitute.For<IOddsStore>();
    private readonly IOutboxWriter _outbox = Substitute.For<IOutboxWriter>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IClock _clock = Substitute.For<IClock>();
    private readonly Match _match = LiveMatch();
    private readonly PlaceCouponHandler _handler;

    public PlaceCouponHandlerTests()
    {
        _clock.UtcNow.Returns(new DateTimeOffset(2026, 1, 1, 12, 0, 0, TimeSpan.Zero));
        _matches.GetByIdsAsync(Arg.Any<IReadOnlyCollection<Guid>>(), Arg.Any<CancellationToken>())
            .Returns([_match]);
        _handler = new PlaceCouponHandler(_coupons, _matches, _oddsStore, _outbox, _unitOfWork, _clock);
    }

    [Fact]
    public async Task PlacesCoupon_WhenOddsAreUnchanged()
    {
        _oddsStore.GetAsync(_match.Id, Arg.Any<CancellationToken>()).Returns(_match.Odds);

        var result = await _handler.HandleAsync(Command(_match.Odds.Home), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.AlreadyExisted.Should().BeFalse();
        result.Value.Coupon.TotalOdds.Should().Be(_match.Odds.Home);
        _coupons.Received(1).Add(Arg.Any<Coupon>());
        _outbox.Received(1).Add(Arg.Any<CouponPlacedMessage>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UsesRedisOddsInsteadOfDatabaseOdds()
    {
        var redisOdds = MatchOdds.Create(1.60m, 3.30m, 5.00m);
        _oddsStore.GetAsync(_match.Id, Arg.Any<CancellationToken>()).Returns(redisOdds);

        var result = await _handler.HandleAsync(Command(redisOdds.Home), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task ReturnsOddsChanged_AndSavesNothing_WhenOddsMoved()
    {
        var newOdds = MatchOdds.Create(1.60m, 3.30m, 5.00m);
        _oddsStore.GetAsync(_match.Id, Arg.Any<CancellationToken>()).Returns(newOdds);

        var result = await _handler.HandleAsync(Command(2.00m), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        var error = result.Error.Should().BeOfType<OddsChangedError>().Subject;
        error.Changes.Should().ContainSingle();
        error.Changes[0].ClientOdds.Should().Be(2.00m);
        error.Changes[0].CurrentOdds.Home.Should().Be(1.60m);
        _coupons.DidNotReceive().Add(Arg.Any<Coupon>());
        _outbox.DidNotReceive().Add(Arg.Any<CouponPlacedMessage>());
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ReturnsExistingCoupon_WhenIdempotencyKeyWasUsed()
    {
        var existing = Coupon.Create(
            Key,
            10m,
            [CouponSelection.Create(_match, CouponPick.Home, 2.00m).Value],
            _clock.UtcNow).Value;
        _coupons.GetByIdempotencyKeyAsync(Key, Arg.Any<CancellationToken>()).Returns(existing);

        var result = await _handler.HandleAsync(Command(2.00m), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.AlreadyExisted.Should().BeTrue();
        result.Value.Coupon.Id.Should().Be(existing.Id);
        _coupons.DidNotReceive().Add(Arg.Any<Coupon>());
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ReturnsWinnerCoupon_WhenParallelRequestWithSameKeySavedFirst()
    {
        _oddsStore.GetAsync(_match.Id, Arg.Any<CancellationToken>()).Returns(_match.Odds);
        var winner = Coupon.Create(
            Key,
            10m,
            [CouponSelection.Create(_match, CouponPick.Home, _match.Odds.Home).Value],
            _clock.UtcNow).Value;
        _coupons.GetByIdempotencyKeyAsync(Key, Arg.Any<CancellationToken>()).Returns(null, winner);
        _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(_ => throw new DuplicateKeyException(new Exception()));

        var result = await _handler.HandleAsync(Command(_match.Odds.Home), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.AlreadyExisted.Should().BeTrue();
        result.Value.Coupon.Id.Should().Be(winner.Id);
    }

    [Fact]
    public async Task ReturnsNotFound_WhenMatchDoesNotExist()
    {
        _matches.GetByIdsAsync(Arg.Any<IReadOnlyCollection<Guid>>(), Arg.Any<CancellationToken>())
            .Returns([]);

        var result = await _handler.HandleAsync(Command(2.00m), CancellationToken.None);

        result.Error.Should().Be(MatchErrors.NotFound);
    }

    [Fact]
    public async Task RejectsFinishedMatch_BeforeComparingOdds()
    {
        _match.Apply(new MatchEvent(_match.Id, MatchEventType.FullTime, null, 90, _clock.UtcNow));

        var result = await _handler.HandleAsync(Command(2.00m), CancellationToken.None);

        result.Error.Should().Be(CouponErrors.MatchFinished);
    }

    [Fact]
    public async Task ReturnsStakeError_WhenStakeIsInvalid()
    {
        _oddsStore.GetAsync(_match.Id, Arg.Any<CancellationToken>()).Returns(_match.Odds);
        var command = Command(_match.Odds.Home) with { Stake = 0m };

        var result = await _handler.HandleAsync(command, CancellationToken.None);

        result.Error.Should().Be(CouponErrors.InvalidStake);
    }

    private PlaceCouponCommand Command(decimal homeOdds) =>
        new(Key, 10m, [new CouponSelectionRequest(_match.Id, CouponPick.Home, homeOdds)]);

    private static Match LiveMatch()
    {
        var match = Match.Create(Guid.NewGuid(), "Kartal SK", "Aslan FK", MatchOdds.Create(2.00m, 3.00m, 4.00m));
        match.Apply(new MatchEvent(match.Id, MatchEventType.Kickoff, null, 0, DateTimeOffset.UtcNow));
        return match;
    }
}
