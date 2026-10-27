namespace LivePulse.Application.Coupons;

public sealed record CouponSelectionDto(Guid MatchId, string Pick, decimal Odds);
