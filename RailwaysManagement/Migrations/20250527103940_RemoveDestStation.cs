using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RailwaysManagement.Migrations
{
    /// <inheritdoc />
    public partial class RemoveDestStation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_RouteRequests_Stations_DestinationStationId",
                table: "RouteRequests");

            migrationBuilder.DropIndex(
                name: "IX_RouteRequests_DestinationStationId",
                table: "RouteRequests");

            migrationBuilder.DropColumn(
                name: "DestinationStationId",
                table: "RouteRequests");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DestinationStationId",
                table: "RouteRequests",
                type: "varchar(225)",
                maxLength: 225,
                nullable: false,
                defaultValue: "")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_RouteRequests_DestinationStationId",
                table: "RouteRequests",
                column: "DestinationStationId");

            migrationBuilder.AddForeignKey(
                name: "FK_RouteRequests_Stations_DestinationStationId",
                table: "RouteRequests",
                column: "DestinationStationId",
                principalTable: "Stations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
