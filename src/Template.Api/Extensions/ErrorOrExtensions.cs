using ErrorOr;

namespace Template.Api.Extensions;

public static class ErrorOrExtensions
{
    public static IResult ToApiResult<T>(this ErrorOr<T> result)
    {
        return result.Match(
            value => Results.Ok(value),
            errors => errors.First().Type switch
            {
                ErrorType.NotFound => Results.NotFound(new { errors[0].Description }),
                ErrorType.Validation => Results.BadRequest(new { Errors = errors.Select(e => e.Description) }),
                ErrorType.Conflict => Results.Conflict(new { errors[0].Description }),
                _ => Results.Problem(errors[0].Description)
            });
    }

    public static IResult ToCreatedResult<T>(this ErrorOr<T> result, string routeName, object routeValues)
    {
        return result.Match(
            value => Results.CreatedAtRoute(routeName, routeValues, value),
            errors => errors.First().Type switch
            {
                ErrorType.Validation => Results.BadRequest(new { Errors = errors.Select(e => e.Description) }),
                _ => Results.Problem(errors[0].Description)
            });
    }
}
