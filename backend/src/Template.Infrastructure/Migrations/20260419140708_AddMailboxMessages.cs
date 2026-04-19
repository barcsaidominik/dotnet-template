using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Template.Infrastructure.Migrations;

/// <inheritdoc />
public partial class AddMailboxMessages : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "MailboxMessages",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                RecipientUserId = table.Column<Guid>(type: "uuid", nullable: false),
                Category = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                TitleKey = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                BodyKey = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                ParametersJson = table.Column<string>(type: "text", nullable: true),
                Link = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                IsRead = table.Column<bool>(type: "boolean", nullable: false),
                ReadAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_MailboxMessages", x => x.Id);
            });

        migrationBuilder.CreateIndex(
            name: "IX_MailboxMessages_RecipientUserId_IsRead_CreatedAt",
            table: "MailboxMessages",
            columns: ["RecipientUserId", "IsRead", "CreatedAt"]);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "MailboxMessages");
    }
}
