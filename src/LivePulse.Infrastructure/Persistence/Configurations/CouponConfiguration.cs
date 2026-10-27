using LivePulse.Domain.Coupons;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LivePulse.Infrastructure.Persistence.Configurations;

public sealed class CouponConfiguration : IEntityTypeConfiguration<Coupon>
{
    public void Configure(EntityTypeBuilder<Coupon> builder)
    {
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).ValueGeneratedNever();
        builder.Property(c => c.IdempotencyKey).HasMaxLength(100).IsRequired();
        builder.Property(c => c.Stake).HasPrecision(18, 2);
        builder.Property(c => c.TotalOdds).HasPrecision(18, 2);
        builder.Property(c => c.Status).HasConversion<string>().HasMaxLength(20);
        builder.HasIndex(c => c.IdempotencyKey).IsUnique();

        builder.HasMany(c => c.Selections).WithOne().HasForeignKey("CouponId").IsRequired().OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(c => c.Selections).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
