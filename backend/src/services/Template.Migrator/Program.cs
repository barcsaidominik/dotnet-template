using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Template.Common.Jobs;
using Template.Infrastructure.Persistence;

namespace Template.Migrator;

/// <summary>
/// One-shot migration runner. Applies the <see cref="AppDbContext"/> migrations and exits, so that
/// every API container can wait for an already migrated schema via
/// <c>service_completed_successfully</c> instead of racing each other on startup.
/// </summary>
public static class Program
{
    public static async Task<int> Main()
    {
        // Environment variables only: docker-compose passes ConnectionStrings__DefaultConnection to
        // every service that touches the database. AddDockerSecrets() is deliberately not called -
        // it maps jwt_secret and internal_service_token, and a migrator has no business reading
        // either of those in order to create a table.
        var configuration = new ConfigurationBuilder()
            .AddEnvironmentVariables()
            .Build();

        var connectionString = configuration["ConnectionStrings:DefaultConnection"]
            ?? throw new InvalidOperationException(
                "ConnectionStrings:DefaultConnection is not configured. "
                + "Set the ConnectionStrings__DefaultConnection environment variable.");

        await using var provider = BuildServiceProvider(connectionString);
        await using var scope = provider.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        // No try/catch anywhere in this program. A failure has to surface as a non-zero exit code,
        // otherwise the compose gate would accept a broken schema as a successful migration.
        var pending = (await context.Database.GetPendingMigrationsAsync()).ToList();

        if (pending.Count == 0)
        {
            Log("No pending migrations, the schema is already up to date.");
            return 0;
        }

        Log($"{pending.Count} pending migration(s) to apply:");
        foreach (var migration in pending)
        {
            Log($"  - {migration}");
        }

        await context.Database.MigrateAsync();

        Log($"{pending.Count} migration(s) applied successfully.");
        return 0;
    }

    /// <summary>
    /// Builds the minimal container the migrator needs. AddInfrastructureCore() is deliberately
    /// bypassed: it validates JwtSettings, InternalServiceAuth and Frontend with ValidateOnStart(),
    /// and creating a table does not require a JWT secret. AddBackgroundJobs() on the other hand is
    /// required, because TickerQ maps its tables through an IModelCustomizer rather than through
    /// AppDbContext.OnModelCreating. Without it the model would lack the TickerQ tables that the
    /// migrations do contain, and EF Core would abort with PendingModelChangesWarning. Reusing the
    /// APIs' own registration keeps the model identical to the running services, which keeps that
    /// EF check a real safety net rather than noise.
    /// </summary>
    public static ServiceProvider BuildServiceProvider(string connectionString)
    {
        var services = new ServiceCollection();

        services.AddDbContext<AppDbContext>(options => options.UseNpgsql(connectionString));
        services.AddBackgroundJobs<AppDbContext>();

        return services.BuildServiceProvider();
    }

    private static void Log(string message)
    {
        Console.WriteLine($"[migrator] {message}");
    }
}
