using LivePulse.Application.Matches;
using LivePulse.Domain.Matches;
using Microsoft.Extensions.Options;

namespace LivePulse.Simulator;

public sealed class MatchEventGenerator(IOptions<SimulatorOptions> options)
{
    private readonly SimulatorOptions _options = options.Value;

    public MatchEventMessage? Next(Guid matchId, int minute)
    {
        var occurredAt = DateTimeOffset.UtcNow;

        if (minute >= _options.FullTimeMinute)
        {
            return new MatchEventMessage(matchId, MatchEventType.FullTime, null, _options.FullTimeMinute, occurredAt);
        }

        var roll = Random.Shared.NextDouble();
        if (roll < _options.GoalProbability)
        {
            return new MatchEventMessage(matchId, MatchEventType.Goal, RandomSide(), minute, occurredAt);
        }

        if (roll < _options.GoalProbability + _options.RedCardProbability)
        {
            return new MatchEventMessage(matchId, MatchEventType.RedCard, RandomSide(), minute, occurredAt);
        }

        return null;
    }

    private static MatchSide RandomSide() =>
        Random.Shared.Next(2) == 0 ? MatchSide.Home : MatchSide.Away;
}
