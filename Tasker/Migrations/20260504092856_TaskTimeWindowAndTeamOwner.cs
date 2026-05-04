using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tasker.Migrations
{
    /// <inheritdoc />
    public partial class TaskTimeWindowAndTeamOwner : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "DueDate",
                table: "Tasks",
                newName: "PlannedEndAt");

            migrationBuilder.AddColumn<int>(
                name: "OwnerId",
                table: "Teams",
                type: "int",
                nullable: false,
                defaultValue: 0);

            // Backfill OwnerId for existing teams:
            // - Prefer any existing member of the team (lowest user id).
            // - If a team has no members, fall back to the lowest user id in Users.
            migrationBuilder.Sql(@"
UPDATE t
SET t.OwnerId = tu.MembersId
FROM Teams t
INNER JOIN (
    SELECT TeamsId, MIN(MembersId) AS MembersId
    FROM TeamUser
    GROUP BY TeamsId
) tu ON tu.TeamsId = t.Id
WHERE t.OwnerId = 0;

UPDATE Teams
SET OwnerId = (SELECT TOP(1) Id FROM Users ORDER BY Id)
WHERE OwnerId = 0;
");

            migrationBuilder.AddColumn<DateTime>(
                name: "CompletedAt",
                table: "Tasks",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "PlannedStartAt",
                table: "Tasks",
                type: "datetime2",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Teams_OwnerId",
                table: "Teams",
                column: "OwnerId");

            migrationBuilder.AddForeignKey(
                name: "FK_Teams_Users_OwnerId",
                table: "Teams",
                column: "OwnerId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Teams_Users_OwnerId",
                table: "Teams");

            migrationBuilder.DropIndex(
                name: "IX_Teams_OwnerId",
                table: "Teams");

            migrationBuilder.DropColumn(
                name: "OwnerId",
                table: "Teams");

            migrationBuilder.DropColumn(
                name: "CompletedAt",
                table: "Tasks");

            migrationBuilder.DropColumn(
                name: "PlannedStartAt",
                table: "Tasks");

            migrationBuilder.RenameColumn(
                name: "PlannedEndAt",
                table: "Tasks",
                newName: "DueDate");
        }
    }
}
