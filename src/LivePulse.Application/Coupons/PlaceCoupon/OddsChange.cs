using LivePulse.Application.Matches;
using LivePulse.Domain.Coupons;

namespace LivePulse.Application.Coupons.PlaceCoupon;

public sealed record OddsChange(Guid MatchId, CouponPick Pick, decimal ClientOdds, OddsDto CurrentOdds);
