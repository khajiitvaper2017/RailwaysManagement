using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RailwaysManagement.Migrations
{
    /// <inheritdoc />
    public partial class StationName : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AspNetUsers_Stations_SourceStationId",
                table: "AspNetUsers");

            migrationBuilder.RenameColumn(
                name: "SourceStationId",
                table: "AspNetUsers",
                newName: "AssignedStationId");

            migrationBuilder.RenameIndex(
                name: "IX_AspNetUsers_SourceStationId",
                table: "AspNetUsers",
                newName: "IX_AspNetUsers_AssignedStationId");

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUsers_Stations_AssignedStationId",
                table: "AspNetUsers",
                column: "AssignedStationId",
                principalTable: "Stations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AspNetUsers_Stations_AssignedStationId",
                table: "AspNetUsers");

            migrationBuilder.RenameColumn(
                name: "AssignedStationId",
                table: "AspNetUsers",
                newName: "SourceStationId");

            migrationBuilder.RenameIndex(
                name: "IX_AspNetUsers_AssignedStationId",
                table: "AspNetUsers",
                newName: "IX_AspNetUsers_SourceStationId");

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUsers_Stations_SourceStationId",
                table: "AspNetUsers",
                column: "SourceStationId",
                principalTable: "Stations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
