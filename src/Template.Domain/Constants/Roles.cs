namespace Template.Domain.Constants;

public static class Roles {
    public const string SystemAdmin = "SystemAdmin";
    public const string FacilityAdmin = "FacilityAdmin";
    public const string FacilityEditor = "FacilityEditor";
    public const string FacilityViewer = "FacilityViewer";

    public static readonly string[] FacilityRoles =
        [FacilityAdmin, FacilityEditor, FacilityViewer];
}
