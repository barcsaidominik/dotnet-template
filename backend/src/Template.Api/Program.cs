using System.Text;
using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using QuestPDF.Infrastructure;
using Scalar.AspNetCore;
using Serilog;
using Template.Api.Logging;
using Template.Api.OpenApi;
using Template.Application;
using Template.Domain.Constants;
using Template.Infrastructure;
using Template.Infrastructure.Persistence;
using TickerQ.Dashboard.DependencyInjection;
using TickerQ.DependencyInjection;
using TickerQ.EntityFrameworkCore.Customizer;
using TickerQ.EntityFrameworkCore.DependencyInjection;

namespace Template.Api;

public class Program
{
    public static async Task Main(string[] args)
    {
        QuestPDF.Settings.License = LicenseType.Community;

        Log.Logger = new LoggerConfiguration()
            .WriteTo.Console()
            .CreateBootstrapLogger();

        try
        {
            var builder = WebApplication.CreateBuilder(args);

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
            builder.Services.AddTickerQ(options =>
            {
                options.ConfigureScheduler(schedulerOptions =>
                {
                    schedulerOptions.MaxConcurrency = 4;
                    schedulerOptions.NodeIdentifier = Environment.MachineName;
                    schedulerOptions.SchedulerTimeZone = TimeZoneInfo.Utc;
                });

                options.AddOperationalStore(efOptions =>
                {
                    efOptions.UseApplicationDbContext<AppDbContext>(ConfigurationType.UseModelCustomizer);
                    efOptions.SetDbContextPoolSize(16);
                });

                options.AddDashboard(dashboardOptions =>
                {
                    dashboardOptions.SetBasePath("/admin/tickerq");
                    dashboardOptions.WithHostAuthentication("TickerQDashboard");
                });
            });

            builder.Services.AddCors(options =>
            {
                options.AddDefaultPolicy(policy =>
                {
                    policy
                        .WithOrigins("http://localhost:4200")
                        .AllowAnyHeader()
                        .AllowAnyMethod()
                        .AllowCredentials();
                });
            });

            builder.Services.AddControllers();
            builder.Services.AddSingleton<CriticalLoadState>();
            builder.Services.Configure<ResourceGuardOptions>(
                builder.Configuration.GetSection(ResourceGuardOptions.SECTION_NAME));
            builder.Services.Configure<RequestTelemetryOptions>(
                builder.Configuration.GetSection(RequestTelemetryOptions.SECTION_NAME));

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

            var jwtSettings = builder.Configuration.GetSection("JwtSettings");
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
                        ValidIssuer = jwtSettings["Issuer"],
                        ValidAudience = jwtSettings["Audience"],
                        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings["Secret"]!))
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

            app.UseCors();
            app.UseMiddleware<RequestTelemetryMiddleware>();
            app.UseMiddleware<ResourceGuardMiddleware>();
            app.UseAuthentication();
            app.UseAuthorization();
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
