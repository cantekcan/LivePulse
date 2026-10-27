using LivePulse.Domain.Common;
using LivePulse.Domain.Matches;

namespace LivePulse.Domain.Coupons;

public sealed class Coupon
{
    private readonly List<CouponSelection> _selections;

    private Coupon(
        Guid id,
        string idempotencyKey,
        decimal stake,
        List<CouponSelection> selections,
        decimal totalOdds,
        DateTimeOffset createdAt)
    {
        Id = id;
        IdempotencyKey = idempotencyKey;
        Stake = stake;
        _selections = selections;
        TotalOdds = totalOdds;
        CreatedAt = createdAt;
        Status = CouponStatus.Pending;
    }

    // Required by EF Core to materialize the entity.
    private Coupon()
    {
        IdempotencyKey = null!;
        _selections = [];
    }

    public Guid Id { get; private set; }

    public string IdempotencyKey { get; private set; }

    public decimal Stake { get; private set; }

    public IReadOnlyList<CouponSelection> Selections => _selections;

    public decimal TotalOdds { get; private set; }

    public CouponStatus Status { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public static Result<Coupon> Create(
        string idempotencyKey,
        decimal stake,
        IReadOnlyList<CouponSelection> selections,
        DateTimeOffset createdAt)
    {
        var error = Validate(idempotencyKey, stake, selections);
        if (error is not null)
        {
            return Result<Coupon>.Failure(error);
        }

        var totalOdds = CalculateTotalOdds(selections);
        var coupon = new Coupon(Guid.NewGuid(), idempotencyKey, stake, selections.ToList(), totalOdds, createdAt);
        return Result<Coupon>.Success(coupon);
    }

    private static Error? Validate(string idempotencyKey, decimal stake, IReadOnlyList<CouponSelection> selections)
    {
        if (string.IsNullOrWhiteSpace(idempotencyKey))
        {
            return CouponErrors.IdempotencyKeyRequired;
        }

        if (selections.Count is < CouponRules.MinSelections or > CouponRules.MaxSelections)
        {
            return CouponErrors.InvalidSelectionCount;
        }

        if (selections.Select(s => s.MatchId).Distinct().Count() != selections.Count)
        {
            return CouponErrors.DuplicateMatch;
        }

        if (stake is < CouponRules.MinStake or > CouponRules.MaxStake)
        {
            return CouponErrors.InvalidStake;
        }

        return null;
    }

    private static decimal CalculateTotalOdds(IReadOnlyList<CouponSelection> selections)
    {
        var product = selections.Aggregate(1m, (total, selection) => total * selection.Odds);
        return Math.Round(product, OddsRules.Decimals, MidpointRounding.AwayFromZero);
    }
}
