using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Template.Infrastructure.Migrations;

/// <inheritdoc />
public partial class AddAuditLog : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "AuditLog",
            columns: table => new
            {
                Id = table.Column<long>(type: "bigint", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                EntityType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                EntityId = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: false),
                Action = table.Column<int>(type: "integer", nullable: false),
                UserId = table.Column<Guid>(type: "uuid", nullable: true),
                UserEmail = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                ChangesJson = table.Column<string>(type: "text", nullable: true),
                OccurredAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_AuditLog", x => x.Id);
            });

        migrationBuilder.CreateIndex(
            name: "IX_AuditLog_EntityType_EntityId",
            table: "AuditLog",
            columns: ["EntityType", "EntityId"]);

        migrationBuilder.CreateIndex(
            name: "IX_AuditLog_OccurredAt",
            table: "AuditLog",
            column: "OccurredAt");

        migrationBuilder.CreateIndex(
            name: "IX_AuditLog_UserId",
            table: "AuditLog",
            column: "UserId");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "AuditLog");
    }
}
