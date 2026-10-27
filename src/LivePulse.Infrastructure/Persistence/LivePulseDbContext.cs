using LivePulse.Application.Abstractions;
using LivePulse.Domain.Coupons;
using LivePulse.Domain.Matches;
using LivePulse.Infrastructure.Outbox;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace LivePulse.Infrastructure.Persistence;

public sealed class LivePulseDbContext(DbContextOptions<LivePulseDbContext> options)
    : DbContext(options), IUnitOfWork
{
    private const int UniqueIndexViolation = 2601;
    private const int UniqueConstraintViolation = 2627;

    public DbSet<Match> Matches => Set<Match>();

    public DbSet<Coupon> Coupons => Set<Coupon>();

    public DbSet<CouponSelection> CouponSelections => Set<CouponSelection>();

    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    async Task IUnitOfWork.SaveChangesAsync(CancellationToken cancellationToken)
    {
        try
        {
            await SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (
            exception.InnerException is SqlException { Number: UniqueIndexViolation or UniqueConstraintViolation })
        {
            throw new DuplicateKeyException(exception);
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder) =>
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(LivePulseDbContext).Assembly);
}
