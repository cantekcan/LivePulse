using LivePulse.Application.Abstractions;
using LivePulse.Application.Matches;
using LivePulse.Application.Telemetry;
using LivePulse.Domain.Common;
using LivePulse.Domain.Coupons;
using LivePulse.Domain.Matches;

namespace LivePulse.Application.Coupons.PlaceCoupon;

public sealed class PlaceCouponHandler(
    ICouponRepository coupons,
    IMatchRepository matches,
    IOddsStore oddsStore,
    IOutboxWriter outbox,
    IUnitOfWork unitOfWork,
    IClock clock) : ICommandHandler<PlaceCouponCommand, PlaceCouponResult>
{
    public async Task<Result<PlaceCouponResult>> HandleAsync(PlaceCouponCommand command, CancellationToken cancellationToken)
    {
        var existing = await coupons.GetByIdempotencyKeyAsync(command.IdempotencyKey, cancellationToken);
        if (existing is not null)
        {
            return Result<PlaceCouponResult>.Success(new PlaceCouponResult(CouponDto.From(existing), AlreadyExisted: true));
        }

        var matchesById = await LoadMatchesAsync(command, cancellationToken);
        if (matchesById is null)
        {
            return Result<PlaceCouponResult>.Failure(MatchErrors.NotFound);
        }

        var selections = new List<CouponSelection>();
        foreach (var request in command.Selections)
        {
            var selection = CouponSelection.Create(matchesById[request.MatchId], request.Pick, request.Odds);
            if (selection.IsFailure)
            {
                return Result<PlaceCouponResult>.Failure(selection.Error!);
            }

            selections.Add(selection.Value);
        }

        var changes = await FindOddsChangesAsync(command, matchesById, cancellationToken);
        if (changes.Count > 0)
        {
            LivePulseTelemetry.CouponsRejectedOddsChanged.Add(1);
            return Result<PlaceCouponResult>.Failure(new OddsChangedError(changes));
        }

        var coupon = Coupon.Create(command.IdempotencyKey, command.Stake, selections, clock.UtcNow);
        if (coupon.IsFailure)
        {
            return Result<PlaceCouponResult>.Failure(coupon.Error!);
        }

        coupons.Add(coupon.Value);
        outbox.Add(new CouponPlacedMessage(coupon.Value.Id, coupon.Value.Stake, coupon.Value.TotalOdds, coupon.Value.CreatedAt));

        try
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (DuplicateKeyException)
        {
            // A parallel request with the same idempotency key won the race; its coupon is the answer.
            return await ReturnExistingAsync(command.IdempotencyKey, cancellationToken);
        }

        LivePulseTelemetry.CouponsAccepted.Add(1);
        return Result<PlaceCouponResult>.Success(new PlaceCouponResult(CouponDto.From(coupon.Value), AlreadyExisted: false));
    }

    private async Task<Result<PlaceCouponResult>> ReturnExistingAsync(string idempotencyKey, CancellationToken cancellationToken)
    {
        var existing = await coupons.GetByIdempotencyKeyAsync(idempotencyKey, cancellationToken);
        return Result<PlaceCouponResult>.Success(new PlaceCouponResult(CouponDto.From(existing!), AlreadyExisted: true));
    }

    private async Task<Dictionary<Guid, Match>?> LoadMatchesAsync(PlaceCouponCommand command, CancellationToken cancellationToken)
    {
        var ids = command.Selections.Select(s => s.MatchId).Distinct().ToList();
        var found = await matches.GetByIdsAsync(ids, cancellationToken);
        return found.Count == ids.Count ? found.ToDictionary(m => m.Id) : null;
    }

    private async Task<List<OddsChange>> FindOddsChangesAsync(
        PlaceCouponCommand command,
        Dictionary<Guid, Match> matchesById,
        CancellationToken cancellationToken)
    {
        var changes = new List<OddsChange>();
        foreach (var request in command.Selections)
        {
            var current = await oddsStore.GetAsync(request.MatchId, cancellationToken) ?? matchesById[request.MatchId].Odds;
            if (current.For(request.Pick) != request.Odds)
            {
                changes.Add(new OddsChange(request.MatchId, request.Pick, request.Odds, OddsDto.From(current)));
            }
        }

        return changes;
    }
}
