using LivePulse.Application.Abstractions;
using LivePulse.Domain.Matches;

namespace LivePulse.Application.Matches.ProcessMatchEvent;

public sealed record ProcessMatchEventCommand(
    Guid MatchId,
    MatchEventType Type,
    MatchSide? Side,
    int Minute,
    DateTimeOffset OccurredAt) : ICommand<MatchDto>;
