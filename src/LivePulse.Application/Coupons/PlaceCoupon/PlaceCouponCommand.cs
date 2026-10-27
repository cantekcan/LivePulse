using LivePulse.Application.Abstractions;

namespace LivePulse.Application.Coupons.PlaceCoupon;

public sealed record PlaceCouponCommand(
    string IdempotencyKey,
    decimal Stake,
    IReadOnlyList<CouponSelectionRequest> Selections) : ICommand<PlaceCouponResult>;
