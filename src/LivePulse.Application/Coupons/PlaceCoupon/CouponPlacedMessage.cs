namespace LivePulse.Application.Coupons.PlaceCoupon;

public sealed record CouponPlacedMessage(Guid CouponId, decimal Stake, decimal TotalOdds, DateTimeOffset PlacedAt);
