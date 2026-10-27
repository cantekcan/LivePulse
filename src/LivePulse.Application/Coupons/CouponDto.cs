using LivePulse.Domain.Coupons;

namespace LivePulse.Application.Coupons;

public sealed record CouponDto(
    Guid Id,
    decimal Stake,
    decimal TotalOdds,
    string Status,
    DateTimeOffset CreatedAt,
    IReadOnlyList<CouponSelectionDto> Selections)
{
    public static CouponDto From(Coupon coupon) => new(
        coupon.Id,
        coupon.Stake,
        coupon.TotalOdds,
        coupon.Status.ToString(),
        coupon.CreatedAt,
        coupon.Selections.Select(s => new CouponSelectionDto(s.MatchId, s.Pick.ToString(), s.Odds)).ToList());
}
