using LivePulse.Application.Abstractions;
using LivePulse.Application.Matches.Rotation;
using LivePulse.Application.Telemetry;
using LivePulse.Domain.Common;
using LivePulse.Domain.Matches;

namespace LivePulse.Application.Matches.ProcessMatchEvent;

public sealed class ProcessMatchEventHandler(
    IMatchRepository matches,
    IUnitOfWork unitOfWork,
    IOddsStore oddsStore,
    IOddsNotifier notifier,
    MatchRotator rotator) : ICommandHandler<ProcessMatchEventCommand, MatchDto>
{
    public async Task<Result<MatchDto>> HandleAsync(ProcessMatchEventCommand command, CancellationToken cancellationToken)
    {
        var match = await matches.GetByIdAsync(command.MatchId, cancellationToken);
        if (match is null)
        {
            return Result<MatchDto>.Failure(MatchErrors.NotFound);
        }

        var matchEvent = new MatchEvent(command.MatchId, command.Type, command.Side, command.Minute, command.OccurredAt);
        var applied = match.Apply(matchEvent);
        if (applied.IsFailure)
        {
            return Result<MatchDto>.Failure(applied.Error!);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        await oddsStore.SetAsync(match.Id, match.Odds, cancellationToken);

        var dto = MatchDto.From(match, match.Odds);
        await notifier.NotifyAsync(dto, cancellationToken);
        LivePulseTelemetry.MatchEventsProcessed.Add(1, new KeyValuePair<string, object?>("type", command.Type.ToString()));

        if (match.Status == MatchStatus.Finished)
        {
            await rotator.ReplaceFinishedMatchAsync(cancellationToken);
        }

        return Result<MatchDto>.Success(dto);
    }
}
