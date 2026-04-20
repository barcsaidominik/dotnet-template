using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;
using Serilog;
using Template.Application;
using Template.Common;
using Template.Common.Email;
using Template.Common.Excel;
using Template.Common.Logging;
using Template.Common.OpenApi;
using Template.Common.Pdf;
using Template.Infrastructure;
using Template.Infrastructure.Settings;
using Template.Products.Api.Services;

namespace Template.Products.Api;

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
            builder.Services.AddInfrastructureCore(builder.Configuration, builder.Environment);
            builder.Services.AddEmail(builder.Environment);
            builder.Services.AddExcel();
            builder.Services.AddQuestPdf();
            builder.Services.AddCommonMiddleware(builder.Configuration);
            builder.Services.AddControllers();
            builder.Services.AddGrpc();

            builder.Services.AddOpenApi(options =>
            {
                options.AddDocumentTransformer((document, context, ct) =>
                {
                    document.Info = new()
                    {
                        Title = "Template Products API",
                        Version = "v1",
                        Description = "Dedicated product management service"
                    };
                    return Task.CompletedTask;
                });

                options.AddDocumentTransformer<BearerSecuritySchemeTransformer>();
            });

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
                        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Secret))
                    };
                });

            builder.Services.AddAuthorization();

            var app = builder.Build();

            app.UseMiddleware<RequestTelemetryMiddleware>();
            app.UseMiddleware<ResourceGuardMiddleware>();
            app.UseAuthentication();
            app.UseAuthorization();
            app.MapGrpcService<FacilityProductsGrpcService>();
            app.MapControllers();
            app.MapOpenApi();
            app.MapScalarApiReference();

            await app.RunAsync();
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }
}
