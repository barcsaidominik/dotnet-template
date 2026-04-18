using ErrorOr;

namespace Template.Domain.Errors;

public static class FacilityErrors {
    public static readonly Error NotFound =
        Error.NotFound("Facility.NotFound", "Facility not found");
    public static readonly Error UserNotInFacility =
        Error.Forbidden("Facility.UserNotInFacility", "User does not belong to this facility");
    public static readonly Error UserAlreadyExists =
        Error.Conflict("Facility.UserAlreadyExists", "A user with this email already exists");
}
