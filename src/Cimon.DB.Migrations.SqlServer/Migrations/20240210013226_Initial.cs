using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cimon.DB.Migrations.SqlServer.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BuildConfigurations_CIConnectors_CIConnectorId",
                table: "BuildConfigurations");

            migrationBuilder.DropIndex(
                name: "IX_BuildConfigurations_CIConnectorId",
                table: "BuildConfigurations");

            migrationBuilder.DropIndex(
                name: "IX_BuildConfigurations_ConnectorId",
                table: "BuildConfigurations");

            migrationBuilder.DropColumn(
                name: "CIConnectorId",
                table: "BuildConfigurations");

            migrationBuilder.AlterColumn<string>(
                name: "Branch",
                table: "BuildConfigurations",
                type: "nvarchar(450)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_BuildConfigurations_ConnectorId_Id_Branch",
                table: "BuildConfigurations",
                columns: new[] { "ConnectorId", "Id", "Branch" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_BuildConfigurations_ConnectorId_Id_Branch",
                table: "BuildConfigurations");

            migrationBuilder.AlterColumn<string>(
                name: "Branch",
                table: "BuildConfigurations",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldNullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CIConnectorId",
                table: "BuildConfigurations",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_BuildConfigurations_CIConnectorId",
                table: "BuildConfigurations",
                column: "CIConnectorId");

            migrationBuilder.CreateIndex(
                name: "IX_BuildConfigurations_ConnectorId",
                table: "BuildConfigurations",
                column: "ConnectorId");

            migrationBuilder.AddForeignKey(
                name: "FK_BuildConfigurations_CIConnectors_CIConnectorId",
                table: "BuildConfigurations",
                column: "CIConnectorId",
                principalTable: "CIConnectors",
                principalColumn: "Id");
        }
    }
}
