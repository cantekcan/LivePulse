using LivePulse.Api.Contracts;
using LivePulse.Api.Http;
using LivePulse.Application.Abstractions;
using LivePulse.Application.Coupons;
using LivePulse.Application.Coupons.GetCouponById;
using LivePulse.Application.Coupons.PlaceCoupon;
using Microsoft.AspNetCore.Mvc;

namespace LivePulse.Api.Controllers;

[ApiController]
[Route("api/coupons")]
public class CouponsController(
    ICommandHandler<PlaceCouponCommand, PlaceCouponResult> placeCoupon,
    IQueryHandler<GetCouponByIdQuery, CouponDto> getCoupon) : ControllerBase
{
    public const string IdempotencyKeyHeader = "Idempotency-Key";

    [HttpPost]
    [ProducesResponseType<CouponDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<CouponDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Place(
        [FromHeader(Name = IdempotencyKeyHeader)] string? idempotencyKey,
        PlaceCouponRequest request,
        CancellationToken cancellationToken)
    {
        var selections = request.Selections
            .Select(s => new CouponSelectionRequest(s.MatchId, s.Pick, s.Odds))
            .ToList();
        var command = new PlaceCouponCommand(idempotencyKey ?? string.Empty, request.Stake, selections);

        var result = await placeCoupon.HandleAsync(command, cancellationToken);
        if (result.IsFailure)
        {
            return this.ToProblem(result.Error!);
        }

        var coupon = result.Value.Coupon;
        return result.Value.AlreadyExisted
            ? Ok(coupon)
            : CreatedAtAction(nameof(GetById), new { id = coupon.Id }, coupon);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType<CouponDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await getCoupon.HandleAsync(new GetCouponByIdQuery(id), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : this.ToProblem(result.Error!);
    }
}
