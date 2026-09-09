using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.DependencyInjection;
using Template.Infrastructure.Persistence;
using Template.Migrator;

namespace Template.Tests;

/// <summary>
/// The migrator container is the only thing that applies migrations under docker-compose, and both
/// APIs gate on its exit code. If its model ever drifts from the migration snapshot, EF Core aborts
/// with PendingModelChangesWarning and the whole stack fails to start. These tests reproduce EF's
/// own pre-migration check without touching a database, so the drift shows up here instead of in a
/// container log.
/// </summary>
public class MigratorModelTests
{
    // Never connected to: building a model and diffing it against the snapshot is offline work.
    private const string FAKE_CONNECTION_STRING =
        "Host=localhost;Port=5432;Database=not_used;Username=not_used;Password=not_used";

    [Fact]
    public void MigratorModel_MapsTheTickerQTables_ThatTheMigrationsCreate()
    {
        using var context = CreateContext();

        // TickerQ maps these through an IModelCustomizer, so they only appear when the migrator
        // registers AddBackgroundJobs. The migrations create them in the "ticker" schema.
        var tickerEntities = context.Model.GetEntityTypes()
            .Where(entityType => entityType.GetSchema() == "ticker")
            .ToList();

        tickerEntities.Should().NotBeEmpty(
            "the migrator must build the same model as the APIs, otherwise MigrateAsync aborts "
            + "with PendingModelChangesWarning and the compose gate never opens");
    }

    [Fact]
    public void MigratorModel_HasNoPendingChanges_AgainstTheMigrationSnapshot()
    {
        using var context = CreateContext();

        var migrationsAssembly = context.GetService<IMigrationsAssembly>();
        var snapshot = migrationsAssembly.ModelSnapshot;
        snapshot.Should().NotBeNull("the migrations must ship a model snapshot");

        var snapshotModel = context.GetService<IModelRuntimeInitializer>()
            .Initialize(((IMutableModel)snapshot!.Model).FinalizeModel(), designTime: true, validationLogger: null);

        var differences = context.GetService<IMigrationsModelDiffer>().GetDifferences(
            snapshotModel.GetRelationalModel(),
            context.GetService<IDesignTimeModel>().Model.GetRelationalModel());

        differences.Should().BeEmpty(
            "EF Core refuses to run MigrateAsync() when the model differs from the snapshot; "
            + "add a migration for the outstanding model changes");
    }

    private static AppDbContext CreateContext()
    {
        var provider = Program.BuildServiceProvider(FAKE_CONNECTION_STRING);
        return provider.CreateScope().ServiceProvider.GetRequiredService<AppDbContext>();
    }
}
