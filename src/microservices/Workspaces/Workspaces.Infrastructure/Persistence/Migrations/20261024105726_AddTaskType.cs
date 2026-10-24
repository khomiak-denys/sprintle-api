using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Workspaces.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTaskType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Type",
                table: "Tasks",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Task");

            migrationBuilder.Sql("""
                UPDATE "TaskEvents" te
                SET "Payload" = jsonb_set(te."Payload"::jsonb, '{Type}', to_jsonb('Task'::text))::text
                FROM "Tasks" t
                WHERE te."TaskId" = t."Id" AND te."EventType" = 'TaskCreatedDomainEvent';
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Type",
                table: "Tasks");
        }
    }
}
