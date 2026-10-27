namespace LivePulse.Api.Contracts;

public sealed record PlaceCouponRequest(decimal Stake, IReadOnlyList<SelectionRequest> Selections);
