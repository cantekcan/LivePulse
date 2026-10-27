namespace LivePulse.Application.Coupons.PlaceCoupon;

public sealed record PlaceCouponResult(CouponDto Coupon, bool AlreadyExisted);
