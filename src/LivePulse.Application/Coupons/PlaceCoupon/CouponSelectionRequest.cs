using LivePulse.Domain.Coupons;

namespace LivePulse.Application.Coupons.PlaceCoupon;

public sealed record CouponSelectionRequest(Guid MatchId, CouponPick Pick, decimal Odds);
