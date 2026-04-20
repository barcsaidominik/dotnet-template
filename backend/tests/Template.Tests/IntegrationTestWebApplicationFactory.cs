using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Template.Application.Common.Dtos;
using Template.Application.Common.Interfaces;
using Template.Domain.Constants;
using Template.Domain.Entities;
using Template.Infrastructure.Identity;
using Template.Infrastructure.Persistence;
using Template.Infrastructure.Products;

namespace Template.Tests;

public sealed class IntegrationTestWebApplicationFactory : WebApplicationFactory<Template.Api.Program>
{
    private readonly SemaphoreSlim _resetLock = new(1, 1);
    private readonly string _databaseName = $"template-tests-{Guid.NewGuid():N}";

    public const string DEFAULT_PASSWORD = "Template1234!";
    public const string FACILITY_ADMIN_EMAIL = "facility.admin@test.local";
    public const string SYSTEM_ADMIN_EMAIL = "system.admin@test.local";

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

        builder.ConfigureAppConfiguration((context, configurationBuilder) =>
        {
            Dictionary<string, string?> overrides = new()
            {
                ["ConnectionStrings:DefaultConnection"] = "Host=localhost;Database=template_tests;Username=test;Password=test",
                ["ResourceGuard:Enabled"] = "false"
            };

            configurationBuilder.AddInMemoryCollection(overrides);
        });

        builder.ConfigureServices(services =>
        {
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

            // Replace gRPC FacilityProductUsageService with the local implementation for testing
            services.RemoveAll<IFacilityProductUsageService>();
            services.AddScoped<IFacilityProductUsageService, LocalFacilityProductUsageService>();

            services.AddDbContext<AppDbContext>(options =>
                options.UseInMemoryDatabase(_databaseName));
        });
    }

    public async Task ResetStateAsync(CancellationToken ct = default)
    {
        await _resetLock.WaitAsync(ct);

        try
        {
            using var scope = Services.CreateScope();
            var serviceProvider = scope.ServiceProvider;
            var dbContext = serviceProvider.GetRequiredService<AppDbContext>();
            var roleManager = serviceProvider.GetRequiredService<Microsoft.AspNetCore.Identity.RoleManager<AppRole>>();
            var userManager = serviceProvider.GetRequiredService<Microsoft.AspNetCore.Identity.UserManager<AppUser>>();

            await dbContext.Database.EnsureDeletedAsync(ct);
            await dbContext.Database.EnsureCreatedAsync(ct);

            foreach (var role in new[] { Roles.SYSTEM_ADMIN, Roles.FACILITY_ADMIN, Roles.FACILITY_EDITOR, Roles.FACILITY_VIEWER })
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    await roleManager.CreateAsync(new AppRole(role));
                }
            }

            var facility = Facility.Create("Integration Facility");
            FacilityId = facility.Id;
            dbContext.Facilities.Add(facility);

            var seededProduct = Product.Create("Seeded Product", 55.25m, facility.Id).Value;
            SeededProductId = seededProduct.Id;
            dbContext.Products.Add(seededProduct);

            var systemAdmin = new AppUser
            {
                UserName = SYSTEM_ADMIN_EMAIL,
                Email = SYSTEM_ADMIN_EMAIL,
                IsApproved = true,
                RequiresPasswordChange = false,
                PreferredLanguage = "en-US"
            };
            await userManager.CreateAsync(systemAdmin, DEFAULT_PASSWORD);
            await userManager.AddToRoleAsync(systemAdmin, Roles.SYSTEM_ADMIN);

            var facilityAdmin = new AppUser
            {
                UserName = FACILITY_ADMIN_EMAIL,
                Email = FACILITY_ADMIN_EMAIL,
                FacilityId = facility.Id,
                IsApproved = true,
                RequiresPasswordChange = false,
                PreferredLanguage = "hu-HU"
            };
            await userManager.CreateAsync(facilityAdmin, DEFAULT_PASSWORD);
            await userManager.AddToRoleAsync(facilityAdmin, Roles.FACILITY_ADMIN);
            FacilityAdminUserId = facilityAdmin.Id;

            dbContext.MailboxMessages.AddRange(
                MailboxMessage.Create(facilityAdmin.Id, "mailbox.first", "mailbox.first.title", "mailbox.first.body"),
                MailboxMessage.Create(facilityAdmin.Id, "mailbox.second", "mailbox.second.title", "mailbox.second.body"),
                MailboxMessage.Create(systemAdmin.Id, "mailbox.admin", "mailbox.admin.title", "mailbox.admin.body"));

            await dbContext.SaveChangesAsync(ct);
        }
        finally
        {
            _resetLock.Release();
        }
    }

    public async Task<HttpClient> CreateAuthenticatedClientAsync(string email, string password, CancellationToken ct = default)
    {
        var client = CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        var response = await client.PostAsJsonAsync("/api/auth/login", new
        {
            Email = email,
            Password = password
        }, ct);

        response.EnsureSuccessStatusCode();

        var token = await response.Content.ReadFromJsonAsync<TokenResponse>(cancellationToken: ct)
            ?? throw new InvalidOperationException("Login did not return a token payload.");

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token.Token);
        return client;
    }

    public async Task<TResult> ExecuteDbContextAsync<TResult>(Func<AppDbContext, Task<TResult>> action)
    {
        using var scope = Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        return await action(dbContext);
    }
}
