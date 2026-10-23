using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable
#pragma warning disable CA1861

namespace Workspaces.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTaskNumber : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "LastTaskNumber",
                table: "Workspaces",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Number",
                table: "Tasks",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            // Backfill existing tasks with sequential numbers ({PREFIX}-{1..N}) per workspace ordered by creation date
            migrationBuilder.Sql("""
                UPDATE "Tasks" t
                SET "Number" = COALESCE(NULLIF(UPPER(SUBSTRING(REGEXP_REPLACE(w."Name", '[^a-zA-Z0-9]', '', 'g') FROM 1 FOR 3)), ''), 'TSK') || '-' || n.rn
                FROM (
                    SELECT "Id", "WorkspaceId", ROW_NUMBER() OVER (PARTITION BY "WorkspaceId" ORDER BY "CreatedAt", "Id") AS rn
                    FROM "Tasks"
                ) n
                JOIN "Workspaces" w ON w."Id" = n."WorkspaceId"
                WHERE t."Id" = n."Id";
                """);

            // Update workspace LastTaskNumber counter to the count of tasks allocated
            migrationBuilder.Sql("""
                UPDATE "Workspaces" w
                SET "LastTaskNumber" = COALESCE((
                    SELECT COUNT(*)
                    FROM "Tasks"
                    WHERE "WorkspaceId" = w."Id"
                ), 0);
                """);

            // Patch existing TaskCreatedDomainEvent payloads in TaskEvents with the backfilled Number
            migrationBuilder.Sql("""
                UPDATE "TaskEvents" te
                SET "Payload" = jsonb_set(te."Payload"::jsonb, '{Number}', to_jsonb(t."Number"))::text
                FROM "Tasks" t
                WHERE te."TaskId" = t."Id" AND te."EventType" = 'TaskCreatedDomainEvent';
                """);

            migrationBuilder.CreateIndex(
                name: "IX_Tasks_WorkspaceId_Number",
                table: "Tasks",
                columns: new[] { "WorkspaceId", "Number" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Tasks_WorkspaceId_Number",
                table: "Tasks");

            migrationBuilder.DropColumn(
                name: "LastTaskNumber",
                table: "Workspaces");

            migrationBuilder.DropColumn(
                name: "Number",
                table: "Tasks");
        }
    }
}
