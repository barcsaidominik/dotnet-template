using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Template.Application.Common.Interfaces;
using Template.Application.Common.Notifications;
using Template.Infrastructure.Email;
using Template.Infrastructure.Identity;
using Template.Infrastructure.Notifications;
using Template.Infrastructure.Notifications.Channels;
using Template.Infrastructure.Persistence;
using Template.Infrastructure.Settings;
using Template.Infrastructure.Templating;

namespace Template.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration, IHostEnvironment environment)
    {
        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));

        services.AddIdentity<AppUser, AppRole>(options =>
        {
            options.Password.RequiredLength = 12;
            options.Password.RequireUppercase = true;
            options.Password.RequireDigit = true;
            options.Password.RequireNonAlphanumeric = true;
            options.User.RequireUniqueEmail = true;
            options.SignIn.RequireConfirmedAccount = false;
        })
        .AddEntityFrameworkStores<AppDbContext>()
        .AddDefaultTokenProviders();

        services.AddMemoryCache();
        services.AddHttpContextAccessor();
        services.AddScoped(typeof(IEntityStore<>), typeof(EntityStore<>));
        services.AddScoped<IJwtTokenService, JwtTokenService>();
        services.AddScoped<ICurrentUserService, CurrentUserService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<INotificationService, NotificationService>();
        services.AddScoped<INotificationChannel, EmailNotificationChannel>();
        services.AddSingleton<ITemplateRenderer, EmbeddedScribanTemplateRenderer>();
        services.AddScoped<ISetupInvitationEmailTemplateFactory, SetupInvitationEmailTemplateFactory>();

        services.Configure<EmailSettings>(configuration.GetSection("Email"));

        var frontendSettings = new FrontendSettings();
        configuration.GetSection("Frontend").Bind(frontendSettings);
        services.AddSingleton<IFrontendSettings>(frontendSettings);

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
