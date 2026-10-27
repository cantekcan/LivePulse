using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace LivePulse.Infrastructure.Persistence;

// Only used by "dotnet ef" to create migrations; the real connection string comes from configuration.
public sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<LivePulseDbContext>
{
    private const string DesignTimeConnectionString =
        "Server=localhost;Database=LivePulse;User Id=sa;Password=LivePulse_Pass123!;TrustServerCertificate=True";

    public LivePulseDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<LivePulseDbContext>()
            .UseSqlServer(DesignTimeConnectionString)
            .Options;

        return new LivePulseDbContext(options);
    }
}
