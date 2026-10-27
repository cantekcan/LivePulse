using LivePulse.Application.Abstractions;
using LivePulse.Domain.Coupons;
using Microsoft.EntityFrameworkCore;

namespace LivePulse.Infrastructure.Persistence;

public sealed class CouponRepository(LivePulseDbContext dbContext) : ICouponRepository
{
    public Task<Coupon?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        dbContext.Coupons.Include(c => c.Selections).FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

    public Task<Coupon?> GetByIdempotencyKeyAsync(string idempotencyKey, CancellationToken cancellationToken) =>
        dbContext.Coupons.Include(c => c.Selections)
            .FirstOrDefaultAsync(c => c.IdempotencyKey == idempotencyKey, cancellationToken);

    public void Add(Coupon coupon) => dbContext.Coupons.Add(coupon);
}
