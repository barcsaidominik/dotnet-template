using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Template.Application.Common.Interfaces;
using Template.Application.Common.Notifications;
using Template.Grpc.Products;
using Template.Infrastructure.BackgroundJobs;
using Template.Infrastructure.Email;
using Template.Infrastructure.Excel;
using Template.Infrastructure.Identity;
using Template.Infrastructure.Mailbox;
using Template.Infrastructure.Notifications;
using Template.Infrastructure.Notifications.Channels;
using Template.Infrastructure.Pdf;
using Template.Infrastructure.Persistence;
using Template.Infrastructure.Products;
using Template.Infrastructure.Settings;
using Template.Infrastructure.Templating;

namespace Template.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration, IHostEnvironment environment)
    {
        services.AddOptions<DatabaseSettings>()
            .Bind(configuration.GetSection(DatabaseSettings.SECTION_NAME))
            .ValidateDataAnnotations()
            .Validate(settings => !string.IsNullOrWhiteSpace(settings.DefaultConnection),
                "ConnectionStrings:DefaultConnection must be configured.")
            .ValidateOnStart();

        services.AddOptions<FrontendSettings>()
            .Bind(configuration.GetSection(FrontendSettings.SECTION_NAME))
            .ValidateDataAnnotations()
            .Validate(settings => Uri.TryCreate(settings.BaseUrl, UriKind.Absolute, out _),
                "Frontend:BaseUrl must be a valid absolute URL.")
            .ValidateOnStart();

        services.AddOptions<EmailSettings>()
            .Bind(configuration.GetSection(EmailSettings.SECTION_NAME));

        services.AddDbContext<AppDbContext>((serviceProvider, options) =>
            options.UseNpgsql(serviceProvider.GetRequiredService<IOptions<DatabaseSettings>>().Value.DefaultConnection));

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
        services.AddScoped<IFacilityProductUsageService>(serviceProvider =>
        {
            var mode = configuration["ProductsService:Mode"];
            return string.Equals(mode, "Proxy", StringComparison.OrdinalIgnoreCase)
                ? new GrpcFacilityProductUsageService(
                    serviceProvider.GetRequiredService<FacilityProductsGrpc.FacilityProductsGrpcClient>())
                : new LocalFacilityProductUsageService(
                    serviceProvider.GetRequiredService<IEntityStore<Template.Domain.Entities.Product>>());
        });
        services.AddScoped<IBackgroundJobScheduler, TickerQBackgroundJobScheduler>();
        services.AddScoped<IExcelWorkbookService, ClosedXmlExcelWorkbookService>();
        services.AddScoped<IMailboxService, MailboxService>();
        services.AddScoped<IProductOrderPdfService, ProductOrderPdfService>();
        services.AddScoped<INotificationService, NotificationService>();
        services.AddScoped<INotificationChannel, EmailNotificationChannel>();
        services.AddSingleton<ITemplateRenderer, EmbeddedScribanTemplateRenderer>();
        services.AddScoped<ISetupInvitationEmailTemplateFactory, SetupInvitationEmailTemplateFactory>();

        services.AddSingleton<IFrontendSettings>(serviceProvider =>
            serviceProvider.GetRequiredService<IOptions<FrontendSettings>>().Value);

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
