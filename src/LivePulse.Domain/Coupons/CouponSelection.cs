using LivePulse.Domain.Common;
using LivePulse.Domain.Matches;

namespace LivePulse.Domain.Coupons;

public sealed class CouponSelection
{
    private CouponSelection(Guid matchId, CouponPick pick, decimal odds)
    {
        MatchId = matchId;
        Pick = pick;
        Odds = odds;
    }

    public Guid MatchId { get; private set; }

    public CouponPick Pick { get; private set; }

    public decimal Odds { get; private set; }

    public static Result<CouponSelection> Create(Match match, CouponPick pick, decimal odds) =>
        match.Status == MatchStatus.Finished
            ? Result<CouponSelection>.Failure(CouponErrors.MatchFinished)
            : Result<CouponSelection>.Success(new CouponSelection(match.Id, pick, odds));
}
