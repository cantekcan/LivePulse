using LivePulse.Application.Abstractions;
using LivePulse.Application.Matches.Rotation;
using LivePulse.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LivePulse.Api.Startup;

public static class DatabaseInitializer
{
    private const int MaxAttempts = 15;
    private static readonly TimeSpan RetryDelay = TimeSpan.FromSeconds(4);

    public static async Task InitializeAsync(IServiceProvider services, CancellationToken cancellationToken)
    {
        using var scope = services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<LivePulseDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger(nameof(DatabaseInitializer));

        await MigrateWithRetryAsync(dbContext, logger, cancellationToken);

        var matches = scope.ServiceProvider.GetRequiredService<IMatchRepository>();
        if (!await matches.AnyUnfinishedAsync(cancellationToken))
        {
            await scope.ServiceProvider.GetRequiredService<MatchRotator>().StartNewSeasonAsync(cancellationToken);
        }
    }

    private static async Task MigrateWithRetryAsync(LivePulseDbContext dbContext, ILogger logger, CancellationToken cancellationToken)
    {
        for (var attempt = 1; ; attempt++)
        {
            try
            {
                await dbContext.Database.MigrateAsync(cancellationToken);
                return;
            }
            catch (Exception exception) when (attempt < MaxAttempts)
            {
                logger.LogWarning(exception, "Database is not ready (attempt {Attempt}/{Max})", attempt, MaxAttempts);
                await Task.Delay(RetryDelay, cancellationToken);
            }
        }
    }
}
