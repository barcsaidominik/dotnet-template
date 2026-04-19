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
    } = "Host=localhost;Port=5432;Database=template_db;Username=postgres;Password=postgres";
}
