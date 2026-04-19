using System.ComponentModel.DataAnnotations;
using Template.Application.Common.Interfaces;

namespace Template.Infrastructure.Settings;

public sealed class FrontendSettings : IFrontendSettings
{
    public const string SECTION_NAME = "Frontend";

    [Required]
    [Url]
    public string BaseUrl
    {
        get;
        set;
    } = "http://localhost:4200";
}
