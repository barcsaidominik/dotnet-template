using ErrorOr;
using Microsoft.AspNetCore.Mvc;

namespace Template.Api.Extensions;

public static class ErrorOrExtensions
{
    public static IActionResult ToActionResult<T>(this ErrorOr<T> result)
    {
        return result.Match<IActionResult>(
            value => new OkObjectResult(value),
            errors => MapErrors(errors));
    }

    public static IActionResult ToActionResult<T>(this ErrorOr<T> result, Func<T, IActionResult> onValue)
    {
        return result.Match(
            onValue,
            errors => MapErrors(errors));
    }

    private static IActionResult MapErrors(List<Error> errors)
    {
        var first = errors[0];
        return first.Type switch
        {
            ErrorType.NotFound => new NotFoundObjectResult(new { first.Description }),
            ErrorType.Validation => new BadRequestObjectResult(new { Errors = errors.Select(e => e.Description) }),
            ErrorType.Conflict => new ConflictObjectResult(new { first.Description }),
            ErrorType.Unauthorized => new UnauthorizedObjectResult(new { first.Description }),
            ErrorType.Forbidden => new ObjectResult(new { first.Description }) { StatusCode = StatusCodes.Status403Forbidden },
            _ => new ObjectResult(new { first.Description }) { StatusCode = StatusCodes.Status500InternalServerError }
        };
    }
}
