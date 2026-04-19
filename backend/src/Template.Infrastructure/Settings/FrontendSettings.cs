using Template.Application.Common.Interfaces;

namespace Template.Infrastructure.Settings;

public sealed class FrontendSettings : IFrontendSettings
{
    public string BaseUrl { get; set; } = "http://localhost:4200";
}
