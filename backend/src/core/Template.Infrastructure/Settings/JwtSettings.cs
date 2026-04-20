using System.ComponentModel.DataAnnotations;

namespace Template.Infrastructure.Settings;

public sealed class JwtSettings
{
    public const string SECTION_NAME = "JwtSettings";
    public const string DEVELOPMENT_SECRET = "template-development-jwt-secret-change-via-user-secrets-2026";
    public const int MIN_SECRET_LENGTH = 32;

    [Required]
    [MinLength(MIN_SECRET_LENGTH)]
    public string Secret
    {
        get;
        set;
    } = DEVELOPMENT_SECRET;

    [Required]
    public string Issuer
    {
        get;
        set;
    } = "template-api";

    [Required]
    public string Audience
    {
        get;
        set;
    } = "template-clients";

    [Range(1, 1440)]
    public int ExpiryMinutes
    {
        get;
        set;
    } = 15;
}
