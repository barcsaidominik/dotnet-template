namespace Template.Application.Common;

public static class CacheKeys
{
    public const string ALL_USERS = "users:all";

    public static string FacilityProducts(Guid facilityId)
    {
        return $"products:facility:{facilityId}";
    }
}
