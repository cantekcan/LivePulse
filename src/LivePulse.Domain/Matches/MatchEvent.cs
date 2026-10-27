namespace LivePulse.Domain.Matches;

public sealed record MatchEvent(
    Guid MatchId,
    MatchEventType Type,
    MatchSide? Side,
    int Minute,
    DateTimeOffset OccurredAt);
