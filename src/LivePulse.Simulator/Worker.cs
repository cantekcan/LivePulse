using LivePulse.Application.Abstractions;
using LivePulse.Application.Matches;
using LivePulse.Infrastructure.Messaging;
using Microsoft.Extensions.Options;

namespace LivePulse.Simulator;

public sealed class Worker(
    MatchApiClient matchApi,
    MatchEventGenerator generator,
    IEventPublisher publisher,
    IOptions<SimulatorOptions> options,
    ILogger<Worker> logger) : BackgroundService
{
    private readonly Dictionary<Guid, int> _minutes = [];

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(options.Value.IntervalSeconds));

        try
        {
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                await TickAsync(stoppingToken);
            }
        }
        catch (OperationCanceledException)
        {
        }
    }

    private async Task TickAsync(CancellationToken cancellationToken)
    {
        try
        {
            var match = await PickLiveMatchAsync(cancellationToken);
            if (match is null)
            {
                return;
            }

            var minute = AdvanceMinute(match);
            var matchEvent = generator.Next(match.Id, minute);
            if (matchEvent is null)
            {
                return;
            }

            var payload = MessageSerializer.Serialize(matchEvent);
            await publisher.PublishAsync(nameof(MatchEventMessage), payload, cancellationToken);
            logger.LogInformation("{Type} for {Home} - {Away} at minute {Minute}", matchEvent.Type, match.HomeTeam, match.AwayTeam, minute);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogWarning(exception, "Simulator tick failed, will try again");
        }
    }

    private async Task<MatchDto?> PickLiveMatchAsync(CancellationToken cancellationToken)
    {
        var matches = await matchApi.GetMatchesAsync(cancellationToken);
        var liveMatches = matches.Where(m => m.Status == "Live").ToList();
        return liveMatches.Count == 0 ? null : liveMatches[Random.Shared.Next(liveMatches.Count)];
    }

    private int AdvanceMinute(MatchDto match)
    {
        var current = _minutes.GetValueOrDefault(match.Id, match.Minute);
        var next = current + options.Value.MinutesPerTick;
        _minutes[match.Id] = next;
        return next;
    }
}
