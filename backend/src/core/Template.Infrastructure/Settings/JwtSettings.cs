using System.ComponentModel.DataAnnotations;

namespace Template.Infrastructure.Settings;

public sealed class JwtSettings
{
    public const string SECTION_NAME = "JwtSettings";
    public const int MIN_SECRET_LENGTH = 32;

    [Required]
    [MinLength(MIN_SECRET_LENGTH)]
    public string Secret
    {
        get;
        set;
    } = "mysupersecretjwtkey1234567890abc";

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
