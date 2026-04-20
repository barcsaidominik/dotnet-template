using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Template.Common.Logging;

namespace Template.Common;

public static class DependencyInjection
{
    public static IServiceCollection AddCommonMiddleware(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<CriticalLoadState>();
        services.Configure<ResourceGuardOptions>(configuration.GetSection(ResourceGuardOptions.SECTION_NAME));
        services.Configure<RequestTelemetryOptions>(configuration.GetSection(RequestTelemetryOptions.SECTION_NAME));
        return services;
    }
}
