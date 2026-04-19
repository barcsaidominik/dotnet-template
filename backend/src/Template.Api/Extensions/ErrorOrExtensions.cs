using ErrorOr;
using Microsoft.AspNetCore.Mvc;

namespace Template.Api.Extensions;

public static class ErrorOrExtensions
{
    public static IActionResult ToActionResult<T>(this ErrorOr<T> result)
    {
        return result.Match(
            value => new OkObjectResult(value),
            MapErrors
        );
    }

    public static IActionResult ToActionResult<T>(this ErrorOr<T> result, Func<T, IActionResult> onValue)
    {
        return result.Match(
            onValue,
            MapErrors
        );
    }

    public static async Task<IActionResult> ToActionResultAsync<T>(this Task<ErrorOr<T>> resultTask)
    {
        var result = await resultTask;
        return result.ToActionResult();
    }

    public static async Task<IActionResult> ToActionResultAsync<T>(this Task<ErrorOr<T>> resultTask, Func<T, IActionResult> onValue)
    {
        var result = await resultTask;
        return result.ToActionResult(onValue);
    }

    public static async ValueTask<IActionResult> ToActionResultAsync<T>(this ValueTask<ErrorOr<T>> resultTask)
    {
        var result = await resultTask;
        return result.ToActionResult();
    }

    public static async ValueTask<IActionResult> ToActionResultAsync<T>(this ValueTask<ErrorOr<T>> resultTask, Func<T, IActionResult> onValue)
    {
        var result = await resultTask;
        return result.ToActionResult(onValue);
    }

    private static IActionResult MapErrors(List<Error> errors)
    {
        if (errors is not { Count: > 0 })
        {
            return CreateProblemResult(StatusCodes.Status500InternalServerError, "Error.Unexpected");
        }

        var first = errors[0];
        return first.Type switch
        {
            ErrorType.NotFound => CreateProblemResult(StatusCodes.Status404NotFound, first.Code),
            ErrorType.Validation => CreateValidationProblemResult(errors),
            ErrorType.Conflict => CreateProblemResult(StatusCodes.Status409Conflict, first.Code),
            ErrorType.Unauthorized => CreateProblemResult(StatusCodes.Status401Unauthorized, first.Code),
            ErrorType.Forbidden => CreateProblemResult(StatusCodes.Status403Forbidden, first.Code),
            _ => CreateProblemResult(StatusCodes.Status500InternalServerError, "Error.Unexpected")
        };
    }

    private static ObjectResult CreateProblemResult(int statusCode, string title)
    {
        return new ObjectResult(
            new ProblemDetails()
            {
                Title = title,
                Status = statusCode
            }
        )
        {
            StatusCode = statusCode
        };
    }

    private static BadRequestObjectResult CreateValidationProblemResult(List<Error> errors)
    {
        var errorsByProperty = errors
            .GroupBy(e => e.Code)
            .ToDictionary(
                g => g.Key,
                g => g.Select(e => e.Description).ToArray());

        return new BadRequestObjectResult(
            new ValidationProblemDetails(errorsByProperty)
            {
                Title = "Validation.Failed",
                Status = StatusCodes.Status400BadRequest
            }
        );
    }
}
