using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Template.Common.Email;

public static class DependencyInjection
{
    public static IServiceCollection AddEmail(this IServiceCollection services, IHostEnvironment environment)
    {
        services.AddOptions<EmailSettings>()
            .BindConfiguration(EmailSettings.SECTION_NAME);

        if (environment.IsDevelopment())
        {
            services.AddScoped<IEmailService, FileEmailService>();
        }
        else
        {
            services.AddScoped<IEmailService, SmtpEmailService>();
        }

        return services;
    }
}
