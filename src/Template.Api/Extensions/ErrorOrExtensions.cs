using ErrorOr;
using Microsoft.AspNetCore.Mvc;

namespace Template.Api.Extensions;

public static class ErrorOrExtensions {
    public static IActionResult ToActionResult<T>(this ErrorOr<T> result) {
        return result.Match(
            value => new OkObjectResult(value),
            MapErrors
        );
    }

    public static IActionResult ToActionResult<T>(this ErrorOr<T> result, Func<T, IActionResult> onValue) {
        return result.Match(
            onValue,
            MapErrors
        );
    }

    public static async Task<IActionResult> ToActionResultAsync<T>(this Task<ErrorOr<T>> resultTask) {
        var result = await resultTask;
        return result.ToActionResult();
    }

    public static async Task<IActionResult> ToActionResultAsync<T>(this Task<ErrorOr<T>> resultTask, Func<T, IActionResult> onValue) {
        var result = await resultTask;
        return result.ToActionResult(onValue);
    }

    public static async ValueTask<IActionResult> ToActionResultAsync<T>(this ValueTask<ErrorOr<T>> resultTask) {
        var result = await resultTask;
        return result.ToActionResult();
    }

    public static async ValueTask<IActionResult> ToActionResultAsync<T>(this ValueTask<ErrorOr<T>> resultTask, Func<T, IActionResult> onValue) {
        var result = await resultTask;
        return result.ToActionResult(onValue);
    }

    private static IActionResult MapErrors(List<Error> errors) {
        if (errors is not { Count: > 0 }) {
            return CreateProblemResult(StatusCodes.Status500InternalServerError, "Unexpected error occurred.");
        }

        var first = errors[0];
        return first.Type switch {
            ErrorType.NotFound => CreateProblemResult(StatusCodes.Status404NotFound, first.Description),
            ErrorType.Validation => CreateValidationProblemResult(errors),
            ErrorType.Conflict => CreateProblemResult(StatusCodes.Status409Conflict, first.Description),
            ErrorType.Unauthorized => CreateProblemResult(StatusCodes.Status401Unauthorized, first.Description),
            ErrorType.Forbidden => CreateProblemResult(StatusCodes.Status403Forbidden, first.Description),
            _ => CreateProblemResult(StatusCodes.Status500InternalServerError, first.Description)
        };
    }

    private static ObjectResult CreateProblemResult(int statusCode, string title) {
        return new ObjectResult(
            new ProblemDetails() {
                Title = title,
                Status = statusCode
            }
        ) {
            StatusCode = statusCode
        };
    }

    private static BadRequestObjectResult CreateValidationProblemResult(List<Error> errors) {
        return new BadRequestObjectResult(
            new ValidationProblemDetails(
                new Dictionary<string, string[]>() {
                    ["errors"] = errors.Select(e => e.Description).ToArray()
                }
            ) {
                Title = "Validation failed.",
                Status = StatusCodes.Status400BadRequest
            }
        );
    }
}
