using LivePulse.Domain.Matches;

namespace LivePulse.Application.Matches;

public sealed record MatchEventMessage(
    Guid MatchId,
    MatchEventType Type,
    MatchSide? Side,
    int Minute,
    DateTimeOffset OccurredAt);
