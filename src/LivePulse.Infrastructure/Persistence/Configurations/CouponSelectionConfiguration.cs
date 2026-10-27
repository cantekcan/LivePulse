using LivePulse.Domain.Coupons;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LivePulse.Infrastructure.Persistence.Configurations;

public sealed class CouponSelectionConfiguration : IEntityTypeConfiguration<CouponSelection>
{
    public void Configure(EntityTypeBuilder<CouponSelection> builder)
    {
        builder.Property<int>("Id");
        builder.HasKey("Id");
        builder.Property(s => s.Pick).HasConversion<string>().HasMaxLength(10);
        builder.Property(s => s.Odds).HasPrecision(8, 2);
    }
}
