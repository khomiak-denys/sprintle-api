using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional
#pragma warning disable CA1861 // Prefer 'static readonly' fields over constant array arguments

namespace Users.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SeedAdminUser : Migration
    {
        private static readonly Guid AdminUserId = new("01a13940-095f-7817-a7a3-da4ba474a63a");
        private static readonly Guid AdminRoleId = new("a1111111-1111-1111-1111-111111111111");

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "AspNetUsers",
                columns: new[]
                {
                    "Id",
                    "AccessFailedCount",
                    "ConcurrencyStamp",
                    "Email",
                    "EmailConfirmed",
                    "FullName",
                    "LockoutEnabled",
                    "LockoutEnd",
                    "NormalizedEmail",
                    "NormalizedUserName",
                    "PasswordHash",
                    "PhoneNumber",
                    "PhoneNumberConfirmed",
                    "SecurityStamp",
                    "TwoFactorEnabled",
                    "UserName"
                },
                values: new object[,]
                {
                    {
                        AdminUserId,
                        0,
                        "7d8e367e-09e9-45b8-80e7-37826e9c4dec",
                        "admin@gmail.com",
                        false,
                        null,
                        true,
                        null,
                        "ADMIN@GMAIL.COM",
                        "ADMIN",
                        "AQAAAAIAAYagAAAAEEvxEc/5S22TNHLeBFSJpDsmvN7E0R7wS83xYRBvwHEPehe26TFFVLIM8wbMzZN1Cg==",
                        null,
                        false,
                        "QCDI2365VDV6ZD2LXWE3HGGPZ35PUFKH",
                        false,
                        "admin"
                    }
                });

            migrationBuilder.InsertData(
                table: "AspNetUserRoles",
                columns: new[] { "RoleId", "UserId" },
                values: new object[,]
                {
                    { AdminRoleId, AdminUserId }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "AspNetUserRoles",
                keyColumns: new[] { "RoleId", "UserId" },
                keyValues: new object[] { AdminRoleId, AdminUserId });

            migrationBuilder.DeleteData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: AdminUserId);
        }
    }
}
