using LivePulse.Application.Coupons.PlaceCoupon;
using LivePulse.Domain.Common;
using Microsoft.AspNetCore.Mvc;

namespace LivePulse.Api.Http;

public static class ErrorResults
{
    public static IActionResult ToProblem(this ControllerBase controller, Error error)
    {
        var status = error.Type switch
        {
            ErrorType.NotFound => StatusCodes.Status404NotFound,
            ErrorType.Conflict => StatusCodes.Status409Conflict,
            _ => StatusCodes.Status400BadRequest
        };

        var problem = new ProblemDetails
        {
            Status = status,
            Title = error.Message,
            Instance = controller.Request.Path
        };
        problem.Extensions["code"] = error.Code;

        if (error is OddsChangedError oddsChanged)
        {
            problem.Extensions["changes"] = oddsChanged.Changes;
        }

        return new ObjectResult(problem) { StatusCode = status };
    }
}
