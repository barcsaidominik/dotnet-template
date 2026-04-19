using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using QuestPDF.Infrastructure;
using Scalar.AspNetCore;
using Serilog;
using Template.Application;
using Template.Infrastructure;
using Template.Infrastructure.Settings;
using Template.Products.Api.Services;

namespace Template.Products.Api;

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
                    .Enrich.WithMachineName();
            });

            builder.Services.AddOptions<JwtSettings>()
                .Bind(builder.Configuration.GetSection(JwtSettings.SECTION_NAME))
                .ValidateDataAnnotations()
                .ValidateOnStart();

            builder.Services.AddApplication();
            builder.Services.AddInfrastructure(builder.Configuration, builder.Environment);
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
