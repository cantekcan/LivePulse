namespace LivePulse.Domain.Coupons;

public static class CouponRules
{
    public const int MinSelections = 1;
    public const int MaxSelections = 10;
    public const decimal MinStake = 1m;
    public const decimal MaxStake = 10_000m;
}
