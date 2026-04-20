using System.ComponentModel.DataAnnotations;

namespace Template.Infrastructure.Settings;

public sealed class InternalServiceAuthSettings
{
    public const string SECTION_NAME = "InternalServiceAuth";
    public const string HEADER_NAME = "x-internal-service-token";
    public const int MIN_TOKEN_LENGTH = 32;

    [Required]
    [MinLength(MIN_TOKEN_LENGTH)]
    public string Token
    {
        get;
        set;
    } = string.Empty;
}
