using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tasker.Migrations
{
    /// <inheritdoc />
    public partial class OrganizationOwner : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "OwnerId",
                table: "Organizations",
                type: "int",
                nullable: true);

            migrationBuilder.Sql(@"
UPDATE o
SET OwnerId = x.MinUserId
FROM Organizations o
INNER JOIN (
    SELECT OrganizationId AS OrgId, MIN(Id) AS MinUserId
    FROM Users
    WHERE OrganizationId IS NOT NULL
    GROUP BY OrganizationId
) x ON x.OrgId = o.Id;

UPDATE Organizations
SET OwnerId = (SELECT TOP 1 Id FROM Users ORDER BY Id)
WHERE OwnerId IS NULL;
");

            migrationBuilder.AlterColumn<int>(
                name: "OwnerId",
                table: "Organizations",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Organizations_OwnerId",
                table: "Organizations",
                column: "OwnerId");

            migrationBuilder.AddForeignKey(
                name: "FK_Organizations_Users_OwnerId",
                table: "Organizations",
                column: "OwnerId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Organizations_Users_OwnerId",
                table: "Organizations");

            migrationBuilder.DropIndex(
                name: "IX_Organizations_OwnerId",
                table: "Organizations");

            migrationBuilder.DropColumn(
                name: "OwnerId",
                table: "Organizations");
        }
    }
}
