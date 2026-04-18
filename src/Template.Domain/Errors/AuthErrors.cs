using ErrorOr;

namespace Template.Domain.Errors;

public static class AuthErrors {
    public static readonly Error InvalidCredentials =
        Error.Unauthorized("Auth.InvalidCredentials", "Invalid email or password");
    public static readonly Error NotApproved =
        Error.Forbidden("Auth.NotApproved", "Account is pending approval");
    public static readonly Error NotAssigned =
        Error.Forbidden("Auth.NotAssigned", "Account is not assigned to a facility");
    public static readonly Error PasswordChangeRequired =
        Error.Forbidden("Auth.PasswordChangeRequired", "Password must be changed before logging in");
    public static readonly Error UserNotFound =
        Error.NotFound("Auth.UserNotFound", "User not found");
}
