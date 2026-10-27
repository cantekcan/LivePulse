using LivePulse.Api.Http;
using LivePulse.Application.Abstractions;
using LivePulse.Application.Matches;
using LivePulse.Application.Matches.GetLiveMatches;
using Microsoft.AspNetCore.Mvc;

namespace LivePulse.Api.Controllers;

[ApiController]
[Route("api/matches")]
public class MatchesController(IQueryHandler<GetLiveMatchesQuery, IReadOnlyList<MatchDto>> handler) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<MatchDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetLiveMatches(CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new GetLiveMatchesQuery(), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : this.ToProblem(result.Error!);
    }
}
