namespace Template.Domain.Constants;

public static class Roles
{
    public const string SYSTEM_ADMIN = "SystemAdmin";
    public const string FACILITY_ADMIN = "FacilityAdmin";
    public const string FACILITY_EDITOR = "FacilityEditor";
    public const string FACILITY_VIEWER = "FacilityViewer";

    public static readonly string[] FacilityRoles = [FACILITY_ADMIN, FACILITY_EDITOR, FACILITY_VIEWER];
}
