using System.ComponentModel.DataAnnotations;

namespace Template.Infrastructure.Settings;

public sealed class DatabaseSettings
{
    public const string SECTION_NAME = "ConnectionStrings";

    [Required]
    public string DefaultConnection
    {
        get;
        set;
    } = string.Empty;
}
