using LivePulse.Application.Abstractions;

namespace LivePulse.Application.Coupons.GetCouponById;

public sealed record GetCouponByIdQuery(Guid CouponId) : IQuery<CouponDto>;
