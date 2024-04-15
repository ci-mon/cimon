using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cimon.DB.Migrations.SqlServer.Migrations
{
    /// <inheritdoc />
    public partial class monitor_groups : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Type",
                table: "Monitors",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "ConnectedMonitors",
                columns: table => new
                {
                    SourceMonitorModelId = table.Column<int>(type: "int", nullable: false),
                    ConnectedMonitorModelId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConnectedMonitors", x => new { x.SourceMonitorModelId, x.ConnectedMonitorModelId });
                    table.ForeignKey(
                        name: "FK_ConnectedMonitors_Monitors_ConnectedMonitorModelId",
                        column: x => x.ConnectedMonitorModelId,
                        principalTable: "Monitors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ConnectedMonitors_Monitors_SourceMonitorModelId",
                        column: x => x.SourceMonitorModelId,
                        principalTable: "Monitors",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_ConnectedMonitors_ConnectedMonitorModelId",
                table: "ConnectedMonitors",
                column: "ConnectedMonitorModelId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ConnectedMonitors");

            migrationBuilder.DropColumn(
                name: "Type",
                table: "Monitors");
        }
    }
}
