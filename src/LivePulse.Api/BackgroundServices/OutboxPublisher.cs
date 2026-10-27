using LivePulse.Application.Abstractions;
using LivePulse.Infrastructure.Outbox;
using LivePulse.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using OpenTelemetry;

namespace LivePulse.Api.BackgroundServices;

public sealed class OutboxPublisher(
    IServiceScopeFactory scopeFactory,
    IOptions<OutboxOptions> options,
    ILogger<OutboxPublisher> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var interval = TimeSpan.FromSeconds(options.Value.PollIntervalSeconds);
        using var timer = new PeriodicTimer(interval);

        while (await WaitForNextTickAsync(timer, stoppingToken))
        {
            try
            {
                await PublishPendingAsync(stoppingToken);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                logger.LogWarning(exception, "Outbox publishing failed, will retry on the next tick");
            }
        }
    }

    private async Task PublishPendingAsync(CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<LivePulseDbContext>();
        var publisher = scope.ServiceProvider.GetRequiredService<IEventPublisher>();
        var clock = scope.ServiceProvider.GetRequiredService<IClock>();

        var pending = await LoadPendingAsync(dbContext, cancellationToken);
        if (pending.Count == 0)
        {
            return;
        }

        foreach (var message in pending)
        {
            try
            {
                await publisher.PublishAsync(message.Type, message.Payload, cancellationToken);
                message.MarkProcessed(clock.UtcNow);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                message.MarkFailed(exception.Message);
                logger.LogWarning(exception, "Could not publish outbox message {MessageId}", message.Id);
                break;
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task<List<OutboxMessage>> LoadPendingAsync(LivePulseDbContext dbContext, CancellationToken cancellationToken)
    {
        // The poll runs every few seconds; tracing it would bury the traces we care about.
        using var suppressed = SuppressInstrumentationScope.Begin();
        return await dbContext.OutboxMessages
            .Where(m => m.ProcessedAt == null)
            .OrderBy(m => m.OccurredAt)
            .Take(options.Value.BatchSize)
            .ToListAsync(cancellationToken);
    }

    private static async Task<bool> WaitForNextTickAsync(PeriodicTimer timer, CancellationToken cancellationToken)
    {
        try
        {
            return await timer.WaitForNextTickAsync(cancellationToken);
        }
        catch (OperationCanceledException)
        {
            return false;
        }
    }
}
