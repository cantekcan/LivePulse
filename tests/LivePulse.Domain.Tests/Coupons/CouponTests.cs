using LivePulse.Domain.Coupons;
using LivePulse.Domain.Matches;
using static LivePulse.Domain.Tests.Matches.MatchTestFactory;

namespace LivePulse.Domain.Tests.Coupons;

public class CouponTests
{
    private const string Key = "key-1";

    [Fact]
    public void Create_MultipliesSelectionOddsIntoTotalOdds()
    {
        var selections = new[] { Selection(1.50m), Selection(2.00m), Selection(1.10m) };

        var result = Coupon.Create(Key, 10m, selections, DateTimeOffset.UtcNow);

        result.IsSuccess.Should().BeTrue();
        result.Value.TotalOdds.Should().Be(3.30m);
        result.Value.Status.Should().Be(CouponStatus.Pending);
        result.Value.IdempotencyKey.Should().Be(Key);
    }

    [Fact]
    public void Create_WithoutSelections_Fails()
    {
        var result = Coupon.Create(Key, 10m, [], DateTimeOffset.UtcNow);

        result.Error.Should().Be(CouponErrors.InvalidSelectionCount);
    }

    [Fact]
    public void Create_WithElevenSelections_Fails()
    {
        var selections = Enumerable.Range(0, CouponRules.MaxSelections + 1).Select(_ => Selection(1.50m)).ToList();

        var result = Coupon.Create(Key, 10m, selections, DateTimeOffset.UtcNow);

        result.Error.Should().Be(CouponErrors.InvalidSelectionCount);
    }

    [Fact]
    public void Create_WithTenSelections_Succeeds()
    {
        var selections = Enumerable.Range(0, CouponRules.MaxSelections).Select(_ => Selection(1.10m)).ToList();

        var result = Coupon.Create(Key, 10m, selections, DateTimeOffset.UtcNow);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void Create_WithSameMatchTwice_Fails()
    {
        var match = LiveMatch();
        var first = CouponSelection.Create(match, CouponPick.Home, 2.00m).Value;
        var second = CouponSelection.Create(match, CouponPick.Draw, 3.00m).Value;

        var result = Coupon.Create(Key, 10m, [first, second], DateTimeOffset.UtcNow);

        result.Error.Should().Be(CouponErrors.DuplicateMatch);
    }

    [Theory]
    [InlineData(0.99)]
    [InlineData(0)]
    [InlineData(-5)]
    [InlineData(10000.01)]
    public void Create_WithStakeOutOfRange_Fails(double stake)
    {
        var result = Coupon.Create(Key, (decimal)stake, [Selection(1.50m)], DateTimeOffset.UtcNow);

        result.Error.Should().Be(CouponErrors.InvalidStake);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(10000)]
    public void Create_WithStakeOnTheBoundary_Succeeds(double stake)
    {
        var result = Coupon.Create(Key, (decimal)stake, [Selection(1.50m)], DateTimeOffset.UtcNow);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void Create_WithBlankIdempotencyKey_Fails()
    {
        var result = Coupon.Create(" ", 10m, [Selection(1.50m)], DateTimeOffset.UtcNow);

        result.Error.Should().Be(CouponErrors.IdempotencyKeyRequired);
    }

    [Fact]
    public void Selection_OnFinishedMatch_Fails()
    {
        var match = LiveMatch();
        match.Apply(Event(match, MatchEventType.FullTime, null, minute: 90));

        var result = CouponSelection.Create(match, CouponPick.Home, 2.00m);

        result.Error.Should().Be(CouponErrors.MatchFinished);
    }

    private static CouponSelection Selection(decimal odds) =>
        CouponSelection.Create(LiveMatch(), CouponPick.Home, odds).Value;
}
