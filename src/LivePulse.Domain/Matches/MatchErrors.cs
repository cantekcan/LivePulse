using LivePulse.Domain.Common;

namespace LivePulse.Domain.Matches;

public static class MatchErrors
{
    public static readonly Error AlreadyFinished =
        new("match.already_finished", "The match is finished and does not accept events.");

    public static readonly Error AlreadyStarted =
        new("match.already_started", "The match has already started.");

    public static readonly Error NotLive =
        new("match.not_live", "The match is not live.");

    public static readonly Error WrongMatch =
        new("match.wrong_match", "The event belongs to a different match.");

    public static readonly Error SideRequired =
        new("match.side_required", "Goal and red card events need a side.");

    public static readonly Error NotFound =
        new("match.not_found", "The match was not found.", ErrorType.NotFound);
}
