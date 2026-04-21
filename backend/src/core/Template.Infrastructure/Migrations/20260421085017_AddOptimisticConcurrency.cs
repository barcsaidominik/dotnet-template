using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Template.Infrastructure.Migrations;

/// <inheritdoc />
public partial class AddOptimisticConcurrency : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // No schema changes needed.
        // PostgreSQL xmin is a system column that exists on every table automatically.
        // This migration only updates the EF Core model snapshot to track RowVersion properties.
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // No schema changes to revert.
    }
}
