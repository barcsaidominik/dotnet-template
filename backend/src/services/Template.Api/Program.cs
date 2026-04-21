using System.Text;
using System.Threading.RateLimiting;
using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;
using Serilog;
using Template.Application;
using Template.Common;
using Template.Common.Configuration;
using Template.Common.Email;
using Template.Common.Excel;
using Template.Common.Idempotency;
using Template.Common.Jobs;
using Template.Common.Logging;
using Template.Common.OpenApi;
using Template.Common.Pdf;
using Template.Domain.Constants;
using Template.Infrastructure;
using Template.Infrastructure.Persistence;
using Template.Infrastructure.Settings;
using TickerQ.DependencyInjection;

namespace Template.Api;

public class Program
{
    public static async Task Main(string[] args)
    {
        Log.Logger = new LoggerConfiguration()
            .WriteTo.Console()
            .CreateBootstrapLogger();

        try
        {
            var builder = WebApplication.CreateBuilder(args);

            // Docker secrets support (overrides environment variables if secrets exist)
            builder.Configuration.AddDockerSecrets();

            builder.Host.UseSerilog((context, services, configuration) =>
            {
                configuration
                    .ReadFrom.Configuration(context.Configuration)
                    .ReadFrom.Services(services)
                    .Enrich.FromLogContext()
                    .Enrich.WithMachineName()
                    .Enrich.With<SyslogSeverityEnricher>();
            });

            builder.Services.AddApplication();
            builder.Services.AddInfrastructure(builder.Configuration, builder.Environment);
            builder.Services.AddEmail(builder.Environment);
            builder.Services.AddExcel();
            builder.Services.AddQuestPdf();
            builder.Services.AddCommonMiddleware(builder.Configuration);
            builder.Services.AddBackgroundJobs<AppDbContext>(
                dashboardBasePath: "/admin/tickerq",
                dashboardPolicyName: "TickerQDashboard");

            builder.Services.AddDataProtection()
                .PersistKeysToFileSystem(new DirectoryInfo("/app/keys"))
                .SetApplicationName("template-app");

            if (!builder.Environment.IsDevelopment())
            {
                builder.Services.AddRateLimiter(options =>
                {
                    options.AddPolicy("auth", context =>
                        RateLimitPartition.GetFixedWindowLimiter(
                            partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                            factory: _ => new FixedWindowRateLimiterOptions
                            {
                                PermitLimit = 10,
                                Window = TimeSpan.FromMinutes(1),
                                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                                QueueLimit = 0
                            }));
                    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
                });
            }

            builder.Services.AddControllers();

            builder.Services.AddOpenApi(options =>
            {
                options.AddDocumentTransformer((document, context, ct) =>
                {
                    document.Info = new()
                    {
                        Title = "Template API",
                        Version = "v1",
                        Description = "Template API documentation"
                    };
                    return Task.CompletedTask;
                });

                options.AddDocumentTransformer<BearerSecuritySchemeTransformer>();
            });

            builder.Services.AddHealthChecks()
                .AddNpgSql(
                    builder.Configuration.GetConnectionString("DefaultConnection")!,
                    name: "database",
                    tags: ["ready"]);

            var jwtSettings = builder.Configuration
                .GetRequiredSection(JwtSettings.SECTION_NAME)
                .Get<JwtSettings>() ?? throw new InvalidOperationException("JwtSettings configuration is missing.");
            builder.Services
                .AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                })
                .AddJwtBearer(options =>
                {
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidateAudience = true,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,
                        ValidIssuer = jwtSettings.Issuer,
                        ValidAudience = jwtSettings.Audience,
                        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Secret)),
                        ClockSkew = TimeSpan.Zero
                    };
                });

            builder.Services.AddAuthorization(options =>
            {
                options.AddPolicy("TickerQDashboard", policy =>
                {
                    policy.RequireAuthenticatedUser();
                    policy.RequireRole(Roles.SYSTEM_ADMIN);
                });
            });

            var app = builder.Build();

            try
            {
                using (var scope = app.Services.CreateScope())
                {
                    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                    await context.Database.MigrateAsync();
                    await DbSeeder.SeedRolesAsync(scope.ServiceProvider);
                    await DbSeeder.SeedSystemAdminAsync(scope.ServiceProvider);
                }
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Database initialization skipped - ensure database is available in production");
            }

            app.UseMiddleware<RequestTelemetryMiddleware>();
            app.UseMiddleware<ResourceGuardMiddleware>();
            if (!app.Environment.IsDevelopment())
            {
                app.UseRateLimiter();
            }
            app.UseAuthentication();
            app.UseAuthorization();
            app.UseIdempotency();
            app.UseTickerQ();
            app.MapControllers();

            app.MapOpenApi();
            app.MapScalarApiReference();

            app.MapHealthChecks("/health/live", new HealthCheckOptions
            {
                Predicate = _ => false,
                ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
            });

            app.MapHealthChecks("/health/ready", new HealthCheckOptions
            {
                Predicate = check => check.Tags.Contains("ready"),
                ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
            });

            app.MapHealthChecks("/health", new HealthCheckOptions
            {
                ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
            });

            await app.RunAsync();
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }
}
