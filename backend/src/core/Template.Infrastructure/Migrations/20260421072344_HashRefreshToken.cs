using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Template.Infrastructure.Migrations;

/// <inheritdoc />
public partial class HashRefreshToken : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Clear all existing plain text refresh tokens - they will be invalidated anyway
        // since the code now stores hashes instead of raw tokens
        migrationBuilder.Sql("UPDATE \"AspNetUsers\" SET \"RefreshToken\" = NULL WHERE \"RefreshToken\" IS NOT NULL;");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
    }
}
