using LivePulse.Domain.Common;

namespace LivePulse.Domain.Coupons;

public static class CouponErrors
{
    public static readonly Error IdempotencyKeyRequired =
        new("coupon.idempotency_key_required", "An idempotency key is required.");

    public static readonly Error InvalidSelectionCount =
        new("coupon.invalid_selection_count",
            $"A coupon needs between {CouponRules.MinSelections} and {CouponRules.MaxSelections} selections.");

    public static readonly Error DuplicateMatch =
        new("coupon.duplicate_match", "The same match cannot be selected twice.");

    public static readonly Error MatchFinished =
        new("coupon.match_finished", "A finished match cannot be selected.");

    public static readonly Error InvalidStake =
        new("coupon.invalid_stake",
            $"The stake must be between {CouponRules.MinStake} and {CouponRules.MaxStake}.");

    public static readonly Error NotFound =
        new("coupon.not_found", "The coupon was not found.", ErrorType.NotFound);
}
