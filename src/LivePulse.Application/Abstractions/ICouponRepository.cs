using LivePulse.Domain.Coupons;

namespace LivePulse.Application.Abstractions;

public interface ICouponRepository
{
    Task<Coupon?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<Coupon?> GetByIdempotencyKeyAsync(string idempotencyKey, CancellationToken cancellationToken);

    void Add(Coupon coupon);
}
