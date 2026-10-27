using LivePulse.Domain.Matches;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LivePulse.Infrastructure.Persistence.Configurations;

public sealed class MatchConfiguration : IEntityTypeConfiguration<Match>
{
    public void Configure(EntityTypeBuilder<Match> builder)
    {
        builder.HasKey(m => m.Id);
        builder.Property(m => m.Id).ValueGeneratedNever();
        builder.Property(m => m.HomeTeam).HasMaxLength(100).IsRequired();
        builder.Property(m => m.AwayTeam).HasMaxLength(100).IsRequired();
        builder.Property(m => m.Status).HasConversion<string>().HasMaxLength(20);

        builder.OwnsOne(m => m.Odds, odds =>
        {
            odds.Property(o => o.Home).HasColumnName("OddsHome").HasPrecision(8, 2);
            odds.Property(o => o.Draw).HasColumnName("OddsDraw").HasPrecision(8, 2);
            odds.Property(o => o.Away).HasColumnName("OddsAway").HasPrecision(8, 2);
        });
    }
}
