using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RailwaysManagement.Migrations
{
    /// <inheritdoc />
    public partial class mssqllocal_migration_573 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_RouteParts_RouteRequests_RouteRequestId",
                table: "RouteParts");

            migrationBuilder.DropForeignKey(
                name: "FK_RouteParts_Routes_RouteId",
                table: "RouteParts");

            migrationBuilder.DropTable(
                name: "RouteStations");

            migrationBuilder.DropTable(
                name: "TrainCargos");

            migrationBuilder.DropTable(
                name: "TrainRoutes");

            migrationBuilder.DropPrimaryKey(
                name: "PK_RouteParts",
                table: "RouteParts");

            migrationBuilder.DropIndex(
                name: "IX_RouteParts_RouteId",
                table: "RouteParts");

            migrationBuilder.DropColumn(
                name: "PlannedRoute",
                table: "Routes");

            migrationBuilder.DropColumn(
                name: "RouteRequestId",
                table: "RouteParts");

            migrationBuilder.DropColumn(
                name: "SequenceInRequest",
                table: "RouteParts");

            migrationBuilder.RenameColumn(
                name: "RouteId",
                table: "RouteParts",
                newName: "Id");

            migrationBuilder.AddColumn<string>(
                name: "RouteRequestId",
                table: "Routes",
                type: "varchar(225)",
                maxLength: 225,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "RouteId",
                table: "RouteRequests",
                type: "varchar(225)",
                maxLength: 225,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "PlannedRoute",
                table: "RouteParts",
                type: "longtext",
                nullable: false)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "TrainId",
                table: "RouteParts",
                type: "varchar(225)",
                maxLength: 225,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddPrimaryKey(
                name: "PK_RouteParts",
                table: "RouteParts",
                column: "Id");

            migrationBuilder.CreateTable(
                name: "RailRouteRouteParts",
                columns: table => new
                {
                    RailRouteId = table.Column<string>(type: "varchar(225)", maxLength: 225, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    RoutePartId = table.Column<string>(type: "varchar(225)", maxLength: 225, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    OrderInRailRoute = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RailRouteRouteParts", x => new { x.RailRouteId, x.RoutePartId });
                    table.ForeignKey(
                        name: "FK_RailRouteRouteParts_RouteParts_RoutePartId",
                        column: x => x.RoutePartId,
                        principalTable: "RouteParts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RailRouteRouteParts_Routes_RailRouteId",
                        column: x => x.RailRouteId,
                        principalTable: "Routes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "RoutePartCargo",
                columns: table => new
                {
                    RoutePartId = table.Column<string>(type: "varchar(225)", maxLength: 225, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CargoId = table.Column<string>(type: "varchar(225)", maxLength: 225, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Order = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RoutePartCargo", x => new { x.RoutePartId, x.CargoId });
                    table.ForeignKey(
                        name: "FK_RoutePartCargo_Cargos_CargoId",
                        column: x => x.CargoId,
                        principalTable: "Cargos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RoutePartCargo_RouteParts_RoutePartId",
                        column: x => x.RoutePartId,
                        principalTable: "RouteParts",
                        principalColumn: "Id");
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "RoutePartStations",
                columns: table => new
                {
                    RoutePartId = table.Column<string>(type: "varchar(225)", maxLength: 225, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    StationId = table.Column<string>(type: "varchar(225)", maxLength: 225, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Id = table.Column<int>(type: "int", nullable: false),
                    Order = table.Column<int>(type: "int", nullable: false),
                    ExpectedArrival = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    ExpectedDeparture = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RoutePartStations", x => new { x.RoutePartId, x.StationId });
                    table.ForeignKey(
                        name: "FK_RoutePartStations_RouteParts_RoutePartId",
                        column: x => x.RoutePartId,
                        principalTable: "RouteParts",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_RoutePartStations_Stations_StationId",
                        column: x => x.StationId,
                        principalTable: "Stations",
                        principalColumn: "Id");
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_Routes_RouteRequestId",
                table: "Routes",
                column: "RouteRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_RouteRequests_RouteId",
                table: "RouteRequests",
                column: "RouteId");

            migrationBuilder.CreateIndex(
                name: "IX_RouteParts_TrainId",
                table: "RouteParts",
                column: "TrainId");

            migrationBuilder.CreateIndex(
                name: "IX_RailRouteRouteParts_RoutePartId",
                table: "RailRouteRouteParts",
                column: "RoutePartId");

            migrationBuilder.CreateIndex(
                name: "IX_RoutePartCargo_CargoId",
                table: "RoutePartCargo",
                column: "CargoId");

            migrationBuilder.CreateIndex(
                name: "IX_RoutePartStations_StationId",
                table: "RoutePartStations",
                column: "StationId");

            migrationBuilder.AddForeignKey(
                name: "FK_RouteParts_Trains_TrainId",
                table: "RouteParts",
                column: "TrainId",
                principalTable: "Trains",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_RouteRequests_Routes_RouteId",
                table: "RouteRequests",
                column: "RouteId",
                principalTable: "Routes",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Routes_RouteRequests_RouteRequestId",
                table: "Routes",
                column: "RouteRequestId",
                principalTable: "RouteRequests",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_RouteParts_Trains_TrainId",
                table: "RouteParts");

            migrationBuilder.DropForeignKey(
                name: "FK_RouteRequests_Routes_RouteId",
                table: "RouteRequests");

            migrationBuilder.DropForeignKey(
                name: "FK_Routes_RouteRequests_RouteRequestId",
                table: "Routes");

            migrationBuilder.DropTable(
                name: "RailRouteRouteParts");

            migrationBuilder.DropTable(
                name: "RoutePartCargo");

            migrationBuilder.DropTable(
                name: "RoutePartStations");

            migrationBuilder.DropIndex(
                name: "IX_Routes_RouteRequestId",
                table: "Routes");

            migrationBuilder.DropIndex(
                name: "IX_RouteRequests_RouteId",
                table: "RouteRequests");

            migrationBuilder.DropPrimaryKey(
                name: "PK_RouteParts",
                table: "RouteParts");

            migrationBuilder.DropIndex(
                name: "IX_RouteParts_TrainId",
                table: "RouteParts");

            migrationBuilder.DropColumn(
                name: "RouteRequestId",
                table: "Routes");

            migrationBuilder.DropColumn(
                name: "RouteId",
                table: "RouteRequests");

            migrationBuilder.DropColumn(
                name: "PlannedRoute",
                table: "RouteParts");

            migrationBuilder.DropColumn(
                name: "TrainId",
                table: "RouteParts");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "RouteParts",
                newName: "RouteId");

            migrationBuilder.AddColumn<string>(
                name: "PlannedRoute",
                table: "Routes",
                type: "longtext",
                nullable: false)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "RouteRequestId",
                table: "RouteParts",
                type: "varchar(225)",
                maxLength: 225,
                nullable: false,
                defaultValue: "")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<int>(
                name: "SequenceInRequest",
                table: "RouteParts",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddPrimaryKey(
                name: "PK_RouteParts",
                table: "RouteParts",
                columns: new[] { "RouteRequestId", "RouteId" });

            migrationBuilder.CreateTable(
                name: "RouteStations",
                columns: table => new
                {
                    RouteId = table.Column<string>(type: "varchar(225)", maxLength: 225, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    StationId = table.Column<string>(type: "varchar(225)", maxLength: 225, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ExpectedArrival = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    ExpectedDeparture = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    Order = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RouteStations", x => new { x.RouteId, x.StationId });
                    table.ForeignKey(
                        name: "FK_RouteStations_Routes_RouteId",
                        column: x => x.RouteId,
                        principalTable: "Routes",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_RouteStations_Stations_StationId",
                        column: x => x.StationId,
                        principalTable: "Stations",
                        principalColumn: "Id");
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "TrainCargos",
                columns: table => new
                {
                    TrainId = table.Column<string>(type: "varchar(225)", maxLength: 225, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CargoId = table.Column<string>(type: "varchar(225)", maxLength: 225, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Order = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TrainCargos", x => new { x.TrainId, x.CargoId });
                    table.ForeignKey(
                        name: "FK_TrainCargos_Cargos_CargoId",
                        column: x => x.CargoId,
                        principalTable: "Cargos",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_TrainCargos_Trains_TrainId",
                        column: x => x.TrainId,
                        principalTable: "Trains",
                        principalColumn: "Id");
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "TrainRoutes",
                columns: table => new
                {
                    TrainId = table.Column<string>(type: "varchar(225)", maxLength: 225, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    RouteId = table.Column<string>(type: "varchar(225)", maxLength: 225, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TrainRoutes", x => new { x.TrainId, x.RouteId });
                    table.ForeignKey(
                        name: "FK_TrainRoutes_Routes_RouteId",
                        column: x => x.RouteId,
                        principalTable: "Routes",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_TrainRoutes_Trains_TrainId",
                        column: x => x.TrainId,
                        principalTable: "Trains",
                        principalColumn: "Id");
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_RouteParts_RouteId",
                table: "RouteParts",
                column: "RouteId");

            migrationBuilder.CreateIndex(
                name: "IX_RouteStations_StationId",
                table: "RouteStations",
                column: "StationId");

            migrationBuilder.CreateIndex(
                name: "IX_TrainCargos_CargoId",
                table: "TrainCargos",
                column: "CargoId");

            migrationBuilder.CreateIndex(
                name: "IX_TrainRoutes_RouteId",
                table: "TrainRoutes",
                column: "RouteId");

            migrationBuilder.AddForeignKey(
                name: "FK_RouteParts_RouteRequests_RouteRequestId",
                table: "RouteParts",
                column: "RouteRequestId",
                principalTable: "RouteRequests",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_RouteParts_Routes_RouteId",
                table: "RouteParts",
                column: "RouteId",
                principalTable: "Routes",
                principalColumn: "Id");
        }
    }
}
