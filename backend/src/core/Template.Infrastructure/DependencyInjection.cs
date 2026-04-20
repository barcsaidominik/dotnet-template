using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Template.Application.Common.Interfaces;
using Template.Application.Common.Notifications;
using Template.Common.Templating;
using Template.Grpc.Products;
using Template.Infrastructure.Email;
using Template.Infrastructure.Identity;
using Template.Infrastructure.Mailbox;
using Template.Infrastructure.Notifications;
using Template.Infrastructure.Notifications.Channels;
using Template.Infrastructure.Pdf;
using Template.Infrastructure.Persistence;
using Template.Infrastructure.Products;
using Template.Infrastructure.Settings;

namespace Template.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureCore(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        services.AddOptions<DatabaseSettings>()
            .Bind(configuration.GetSection(DatabaseSettings.SECTION_NAME))
            .ValidateDataAnnotations()
            .Validate(settings => !string.IsNullOrWhiteSpace(settings.DefaultConnection),
                "ConnectionStrings:DefaultConnection must be configured.")
            .ValidateOnStart();

        services.AddOptions<JwtSettings>()
            .Bind(configuration.GetSection(JwtSettings.SECTION_NAME))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddOptions<FrontendSettings>()
            .Bind(configuration.GetSection(FrontendSettings.SECTION_NAME))
            .ValidateDataAnnotations()
            .Validate(settings => Uri.TryCreate(settings.BaseUrl, UriKind.Absolute, out _),
                "Frontend:BaseUrl must be a valid absolute URL.")
            .ValidateOnStart();

        services.AddSingleton<IFrontendSettings>(serviceProvider =>
            serviceProvider.GetRequiredService<IOptions<FrontendSettings>>().Value);

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
            options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
            options.Lockout.MaxFailedAccessAttempts = 5;
            options.Lockout.AllowedForNewUsers = true;
        })
        .AddEntityFrameworkStores<AppDbContext>()
        .AddDefaultTokenProviders();

        services.AddMemoryCache();
        services.AddHttpContextAccessor();
        services.AddScoped(typeof(IEntityStore<>), typeof(EntityStore<>));
        services.AddScoped<IJwtTokenService, JwtTokenService>();
        services.AddScoped<ICurrentUserService, CurrentUserService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IMailboxService, MailboxService>();
        services.AddSingleton<ITemplateRenderer>(
            new EmbeddedScribanTemplateRenderer(typeof(AppDbContext).Assembly));
        services.AddScoped<IProductOrderPdfService, ProductOrderPdfService>();
        services.AddScoped<IFacilityProductUsageService, LocalFacilityProductUsageService>();
        services.AddScoped<INotificationService, NotificationService>();
        services.AddScoped<INotificationChannel, EmailNotificationChannel>();
        services.AddScoped<ISetupInvitationEmailTemplateFactory, SetupInvitationEmailTemplateFactory>();

        return services;
    }

    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        services.AddInfrastructureCore(configuration, environment);

        services.AddOptions<ProductsServiceOptions>()
            .Bind(configuration.GetSection(ProductsServiceOptions.SECTION_NAME))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddGrpcClient<FacilityProductsGrpc.FacilityProductsGrpcClient>((serviceProvider, options) =>
        {
            var serviceOptions = serviceProvider
                .GetRequiredService<IOptions<ProductsServiceOptions>>()
                .Value;

            options.Address = new Uri(serviceOptions.GrpcBaseUrl);
        });

        // Override LocalFacilityProductUsageService with gRPC client for main API
        services.AddScoped<IFacilityProductUsageService, GrpcFacilityProductUsageService>();

        return services;
    }
}
