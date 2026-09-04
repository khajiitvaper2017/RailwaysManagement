using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RailwaysManagement.Migrations
{
    /// <inheritdoc />
    public partial class Dates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_RouteRequests_Stations_SourceStationId",
                table: "RouteRequests");

            migrationBuilder.DropIndex(
                name: "IX_RouteRequests_SourceStationId",
                table: "RouteRequests");

            migrationBuilder.DropColumn(
                name: "SourceStationId",
                table: "RouteRequests");

            migrationBuilder.AddColumn<DateTime>(
                name: "ExpectedArrival",
                table: "RouteStations",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ExpectedDeparture",
                table: "RouteStations",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SourceStationId",
                table: "AspNetUsers",
                type: "varchar(225)",
                maxLength: 225,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_SourceStationId",
                table: "AspNetUsers",
                column: "SourceStationId");

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUsers_Stations_SourceStationId",
                table: "AspNetUsers",
                column: "SourceStationId",
                principalTable: "Stations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AspNetUsers_Stations_SourceStationId",
                table: "AspNetUsers");

            migrationBuilder.DropIndex(
                name: "IX_AspNetUsers_SourceStationId",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "ExpectedArrival",
                table: "RouteStations");

            migrationBuilder.DropColumn(
                name: "ExpectedDeparture",
                table: "RouteStations");

            migrationBuilder.DropColumn(
                name: "SourceStationId",
                table: "AspNetUsers");

            migrationBuilder.AddColumn<string>(
                name: "SourceStationId",
                table: "RouteRequests",
                type: "varchar(225)",
                maxLength: 225,
                nullable: false,
                defaultValue: "")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_RouteRequests_SourceStationId",
                table: "RouteRequests",
                column: "SourceStationId");

            migrationBuilder.AddForeignKey(
                name: "FK_RouteRequests_Stations_SourceStationId",
                table: "RouteRequests",
                column: "SourceStationId",
                principalTable: "Stations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
