using LivePulse.Domain.Coupons;

namespace LivePulse.Api.Contracts;

public sealed record SelectionRequest(Guid MatchId, CouponPick Pick, decimal Odds);
