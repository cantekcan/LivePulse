using LivePulse.Application.Abstractions;
using LivePulse.Domain.Common;
using LivePulse.Domain.Coupons;

namespace LivePulse.Application.Coupons.GetCouponById;

public sealed class GetCouponByIdHandler(ICouponRepository coupons) : IQueryHandler<GetCouponByIdQuery, CouponDto>
{
    public async Task<Result<CouponDto>> HandleAsync(GetCouponByIdQuery query, CancellationToken cancellationToken)
    {
        var coupon = await coupons.GetByIdAsync(query.CouponId, cancellationToken);
        return coupon is null
            ? Result<CouponDto>.Failure(CouponErrors.NotFound)
            : Result<CouponDto>.Success(CouponDto.From(coupon));
    }
}
