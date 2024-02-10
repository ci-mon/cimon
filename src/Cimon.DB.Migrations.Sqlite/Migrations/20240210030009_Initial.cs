using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cimon.DB.Migrations.Sqlite.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CIConnectors",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    CISystem = table.Column<int>(type: "INTEGER", nullable: false),
                    Key = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CIConnectors", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Roles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Roles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Teams",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Teams", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    FullName = table.Column<string>(type: "TEXT", nullable: false),
                    Email = table.Column<string>(type: "TEXT", nullable: true),
                    IsDeactivated = table.Column<bool>(type: "INTEGER", nullable: false),
                    AllowLocalLogin = table.Column<bool>(type: "INTEGER", nullable: false),
                    DefaultMonitorId = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BuildConfigurations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ConnectorId = table.Column<int>(type: "INTEGER", nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    DemoState = table.Column<string>(type: "jsonb", nullable: true),
                    Props = table.Column<string>(type: "jsonb", nullable: true),
                    Key = table.Column<string>(type: "TEXT", nullable: false),
                    Branch = table.Column<string>(type: "TEXT", nullable: true),
                    Name = table.Column<string>(type: "TEXT", nullable: true),
                    IsDefaultBranch = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BuildConfigurations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BuildConfigurations_CIConnectors_ConnectorId",
                        column: x => x.ConnectorId,
                        principalTable: "CIConnectors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CIConnectorSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    CIConnectorId = table.Column<int>(type: "INTEGER", nullable: false),
                    Key = table.Column<string>(type: "TEXT", nullable: false),
                    Value = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CIConnectorSettings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CIConnectorSettings_CIConnectors_CIConnectorId",
                        column: x => x.CIConnectorId,
                        principalTable: "CIConnectors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RoleRole",
                columns: table => new
                {
                    OwnedRolesId = table.Column<int>(type: "INTEGER", nullable: false),
                    RoleId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RoleRole", x => new { x.OwnedRolesId, x.RoleId });
                    table.ForeignKey(
                        name: "FK_RoleRole_Roles_OwnedRolesId",
                        column: x => x.OwnedRolesId,
                        principalTable: "Roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RoleRole_Roles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "Roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TeamTeam",
                columns: table => new
                {
                    ChildTeamsId = table.Column<int>(type: "INTEGER", nullable: false),
                    TeamId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TeamTeam", x => new { x.ChildTeamsId, x.TeamId });
                    table.ForeignKey(
                        name: "FK_TeamTeam_Teams_ChildTeamsId",
                        column: x => x.ChildTeamsId,
                        principalTable: "Teams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TeamTeam_Teams_TeamId",
                        column: x => x.TeamId,
                        principalTable: "Teams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Monitors",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Key = table.Column<string>(type: "TEXT", nullable: false),
                    Title = table.Column<string>(type: "TEXT", nullable: true),
                    Removed = table.Column<bool>(type: "INTEGER", nullable: false),
                    AlwaysOnMonitoring = table.Column<bool>(type: "INTEGER", nullable: false),
                    Shared = table.Column<bool>(type: "INTEGER", nullable: false),
                    OwnerId = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Monitors", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Monitors_Users_OwnerId",
                        column: x => x.OwnerId,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "RoleUser",
                columns: table => new
                {
                    RolesId = table.Column<int>(type: "INTEGER", nullable: false),
                    UsersId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RoleUser", x => new { x.RolesId, x.UsersId });
                    table.ForeignKey(
                        name: "FK_RoleUser_Roles_RolesId",
                        column: x => x.RolesId,
                        principalTable: "Roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RoleUser_Users_UsersId",
                        column: x => x.UsersId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TeamUser",
                columns: table => new
                {
                    TeamsId = table.Column<int>(type: "INTEGER", nullable: false),
                    UsersId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TeamUser", x => new { x.TeamsId, x.UsersId });
                    table.ForeignKey(
                        name: "FK_TeamUser_Teams_TeamsId",
                        column: x => x.TeamsId,
                        principalTable: "Teams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TeamUser_Users_UsersId",
                        column: x => x.UsersId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MonitorBuilds",
                columns: table => new
                {
                    MonitorId = table.Column<int>(type: "INTEGER", nullable: false),
                    BuildConfigId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MonitorBuilds", x => new { x.MonitorId, x.BuildConfigId });
                    table.ForeignKey(
                        name: "FK_MonitorBuilds_BuildConfigurations_BuildConfigId",
                        column: x => x.BuildConfigId,
                        principalTable: "BuildConfigurations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MonitorBuilds_Monitors_MonitorId",
                        column: x => x.MonitorId,
                        principalTable: "Monitors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BuildConfigurations_ConnectorId_Id_Branch",
                table: "BuildConfigurations",
                columns: new[] { "ConnectorId", "Id", "Branch" });

            migrationBuilder.CreateIndex(
                name: "IX_CIConnectorSettings_CIConnectorId",
                table: "CIConnectorSettings",
                column: "CIConnectorId");

            migrationBuilder.CreateIndex(
                name: "IX_MonitorBuilds_BuildConfigId",
                table: "MonitorBuilds",
                column: "BuildConfigId");

            migrationBuilder.CreateIndex(
                name: "IX_Monitors_OwnerId",
                table: "Monitors",
                column: "OwnerId");

            migrationBuilder.CreateIndex(
                name: "IX_RoleRole_RoleId",
                table: "RoleRole",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "IX_RoleUser_UsersId",
                table: "RoleUser",
                column: "UsersId");

            migrationBuilder.CreateIndex(
                name: "IX_TeamTeam_TeamId",
                table: "TeamTeam",
                column: "TeamId");

            migrationBuilder.CreateIndex(
                name: "IX_TeamUser_UsersId",
                table: "TeamUser",
                column: "UsersId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_Name",
                table: "Users",
                column: "Name",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CIConnectorSettings");

            migrationBuilder.DropTable(
                name: "MonitorBuilds");

            migrationBuilder.DropTable(
                name: "RoleRole");

            migrationBuilder.DropTable(
                name: "RoleUser");

            migrationBuilder.DropTable(
                name: "TeamTeam");

            migrationBuilder.DropTable(
                name: "TeamUser");

            migrationBuilder.DropTable(
                name: "BuildConfigurations");

            migrationBuilder.DropTable(
                name: "Monitors");

            migrationBuilder.DropTable(
                name: "Roles");

            migrationBuilder.DropTable(
                name: "Teams");

            migrationBuilder.DropTable(
                name: "CIConnectors");

            migrationBuilder.DropTable(
                name: "Users");
        }
    }
}
