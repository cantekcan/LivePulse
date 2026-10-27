using LivePulse.Domain.Common;

namespace LivePulse.Application.Coupons.PlaceCoupon;

public sealed record OddsChangedError(IReadOnlyList<OddsChange> Changes)
    : Error("coupon.odds_changed", "The odds changed since you saw them.", ErrorType.Conflict);
