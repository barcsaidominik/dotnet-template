using ErrorOr;

namespace Template.Domain.Errors;

public static class FacilityErrors
{
    public static readonly Error NotFound = Error.NotFound("Facility.NotFound", "Facility not found");
    public static readonly Error UserNotInFacility = Error.Forbidden("Facility.UserNotInFacility", "User does not belong to this facility");
    public static readonly Error UserAlreadyExists = Error.Conflict("Facility.UserAlreadyExists", "A user with this email already exists");
    public static readonly Error InvalidName = Error.Validation("Facility.InvalidName", "Facility name cannot be empty.");
    public static readonly Error HasProducts = Error.Conflict("Facility.HasProducts", "Facility cannot be deleted while products still reference it.");
    public static readonly Error AccessDenied = Error.Forbidden("Facility.AccessDenied", "Facility.AccessDenied");
}
