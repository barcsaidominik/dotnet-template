using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using ErrorOr;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using NSubstitute;
using Template.Application.Common.Dtos;
using Template.Application.Common.Interfaces;
using Template.Application.Common.Notifications;
using Template.Domain.Constants;
using Template.Domain.Entities;
using Template.Infrastructure.Persistence;
using Template.Infrastructure.Settings;

namespace Template.Tests;

public sealed class ProductsIntegrationTestWebApplicationFactory : WebApplicationFactory<Template.Products.Api.Program>
{
    private const string TEST_JWT_SECRET = "template-tests-jwt-secret-at-least-32-chars";
    private const string TEST_JWT_ISSUER = "template-api";
    private const string TEST_JWT_AUDIENCE = "template-clients";
    private const string TEST_INTERNAL_SERVICE_TOKEN = "template-tests-internal-service-token-32";
    private readonly string _databaseName = $"products-tests-{Guid.NewGuid():N}";

    public Guid FacilityId
    {
        get;
        private set;
    }
    public Guid SeededProductId
    {
        get;
        private set;
    }
    public Guid FacilityAdminUserId
    {
        get;
        private set;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");

        builder.ConfigureAppConfiguration((_, configBuilder) =>
        {
            configBuilder.AddInMemoryCollection(new Dictionary<string, string?>
            {
                // Only has to be non-empty: DatabaseSettings requires it via ValidateOnStart(),
                // while the DbContext is replaced with the InMemory provider below. No credentials
                // belong here -- nothing ever opens this connection.
                ["ConnectionStrings:DefaultConnection"] = "Host=localhost;Database=products_tests",
                ["JwtSettings:Secret"] = TEST_JWT_SECRET,
                ["InternalServiceAuth:Token"] = TEST_INTERNAL_SERVICE_TOKEN,
                ["ResourceGuard:Enabled"] = "false"
            });
        });

        builder.ConfigureServices(services =>
        {
            services.PostConfigure<JwtSettings>(options =>
            {
                options.Secret = TEST_JWT_SECRET;
                options.Issuer = TEST_JWT_ISSUER;
                options.Audience = TEST_JWT_AUDIENCE;
                options.ExpiryMinutes = 15;
            });

            services.PostConfigure<InternalServiceAuthSettings>(options =>
            {
                options.Token = TEST_INTERNAL_SERVICE_TOKEN;
            });

            services.PostConfigureAll<JwtBearerOptions>(options =>
            {
                options.TokenValidationParameters.ValidIssuer = TEST_JWT_ISSUER;
                options.TokenValidationParameters.ValidAudience = TEST_JWT_AUDIENCE;
                options.TokenValidationParameters.IssuerSigningKey =
                    new SymmetricSecurityKey(Encoding.UTF8.GetBytes(TEST_JWT_SECRET));
            });

            services.RemoveAll<DbContextOptions<AppDbContext>>();
            services.RemoveAll<AppDbContext>();
            services.RemoveAll<IDbContextOptionsConfiguration<AppDbContext>>();

            foreach (var descriptor in services
                .Where(descriptor =>
                    descriptor.ServiceType.Assembly.GetName().Name?.Contains("Npgsql.EntityFrameworkCore.PostgreSQL", StringComparison.Ordinal) == true ||
                    descriptor.ImplementationType?.Assembly.GetName().Name?.Contains("Npgsql.EntityFrameworkCore.PostgreSQL", StringComparison.Ordinal) == true ||
                    descriptor.ImplementationInstance?.GetType().Assembly.GetName().Name?.Contains("Npgsql.EntityFrameworkCore.PostgreSQL", StringComparison.Ordinal) == true ||
                    descriptor.ImplementationFactory?.Method.ReturnType.Assembly.GetName().Name?.Contains("Npgsql.EntityFrameworkCore.PostgreSQL", StringComparison.Ordinal) == true)
                .ToList())
            {
                services.Remove(descriptor);
            }

            // Remove TickerQ hosted services (inherited from Infrastructure via transitive deps)
            foreach (var descriptor in services
                .Where(descriptor =>
                    descriptor.ServiceType == typeof(IHostedService) &&
                    (
                        descriptor.ImplementationType?.FullName?.Contains("TickerQ", StringComparison.Ordinal) == true ||
                        descriptor.ImplementationInstance?.GetType().FullName?.Contains("TickerQ", StringComparison.Ordinal) == true ||
                        descriptor.ImplementationFactory?.Method.ReturnType.FullName?.Contains("TickerQ", StringComparison.Ordinal) == true
                    ))
                .ToList())
            {
                services.Remove(descriptor);
            }

            // Stub out services that Products.Api doesn't use - AddInfrastructureCore doesn't register these
            services.RemoveAll<IFacilityProductUsageService>();
            services.AddScoped(_ => Substitute.For<IFacilityProductUsageService>());
            services.RemoveAll<IBackgroundJobScheduler>();
            services.AddScoped(_ => Substitute.For<IBackgroundJobScheduler>());
            services.RemoveAll<IAuthService>();
            services.AddScoped(_ =>
            {
                var authService = Substitute.For<IAuthService>();
                authService.GetUserByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
                    .Returns(Task.FromResult<ErrorOr<UserDto>>(Error.NotFound("User.NotFound", "User not found")));
                return authService;
            });
            services.RemoveAll<IMailboxService>();
            services.AddScoped(_ => Substitute.For<IMailboxService>());
            services.RemoveAll<INotificationService>();
            services.AddScoped(_ => Substitute.For<INotificationService>());
            services.RemoveAll<IFrontendSettings>();
            services.AddSingleton(_ => Substitute.For<IFrontendSettings>());

            services.AddDbContext<AppDbContext>((serviceProvider, options) =>
                options
                    .UseInMemoryDatabase(_databaseName)
                    .AddInterceptors(serviceProvider.GetRequiredService<AuditInterceptor>()));
        });
    }

    public async Task ResetStateAsync(CancellationToken ct = default)
    {
        FacilityAdminUserId = Guid.NewGuid();

        using var scope = Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        await dbContext.Database.EnsureDeletedAsync(ct);
        await dbContext.Database.EnsureCreatedAsync(ct);

        var facility = Facility.Create("Products Integration Facility");
        FacilityId = facility.Id;
        dbContext.Facilities.Add(facility);

        var seededProduct = Product.Create("Seeded Product", 55.25m, facility.Id).Value;
        SeededProductId = seededProduct.Id;
        dbContext.Products.Add(seededProduct);

        await dbContext.SaveChangesAsync(ct);
    }

    public HttpClient CreateFacilityAdminClient()
    {
        var client = CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", GenerateToken(Roles.FACILITY_ADMIN, FacilityId, FacilityAdminUserId));
        return client;
    }

    public async Task<TResult> ExecuteDbContextAsync<TResult>(Func<AppDbContext, Task<TResult>> action)
    {
        using var scope = Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        return await action(dbContext);
    }

    private static string GenerateToken(string role, Guid facilityId, Guid userId)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(TEST_JWT_SECRET));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        List<Claim> claims =
        [
            new(ClaimTypes.NameIdentifier, userId.ToString()),
            new(ClaimTypes.Role, role),
            new("facilityId", facilityId.ToString())
        ];

        var token = new JwtSecurityToken(
            issuer: TEST_JWT_ISSUER,
            audience: TEST_JWT_AUDIENCE,
            claims: claims,
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
