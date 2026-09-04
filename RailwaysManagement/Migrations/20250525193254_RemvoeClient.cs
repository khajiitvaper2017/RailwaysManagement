using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RailwaysManagement.Migrations
{
    /// <inheritdoc />
    public partial class RemvoeClient : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_RouteRequests_AspNetUsers_ClientId",
                table: "RouteRequests");

            migrationBuilder.DropIndex(
                name: "IX_RouteRequests_ClientId",
                table: "RouteRequests");

            migrationBuilder.DropColumn(
                name: "ClientId",
                table: "RouteRequests");

            migrationBuilder.AddColumn<string>(
                name: "RailwaysManagementUserId",
                table: "RouteRequests",
                type: "varchar(255)",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_RouteRequests_RailwaysManagementUserId",
                table: "RouteRequests",
                column: "RailwaysManagementUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_RouteRequests_AspNetUsers_RailwaysManagementUserId",
                table: "RouteRequests",
                column: "RailwaysManagementUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_RouteRequests_AspNetUsers_RailwaysManagementUserId",
                table: "RouteRequests");

            migrationBuilder.DropIndex(
                name: "IX_RouteRequests_RailwaysManagementUserId",
                table: "RouteRequests");

            migrationBuilder.DropColumn(
                name: "RailwaysManagementUserId",
                table: "RouteRequests");

            migrationBuilder.AddColumn<string>(
                name: "ClientId",
                table: "RouteRequests",
                type: "varchar(225)",
                maxLength: 225,
                nullable: false,
                defaultValue: "")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_RouteRequests_ClientId",
                table: "RouteRequests",
                column: "ClientId");

            migrationBuilder.AddForeignKey(
                name: "FK_RouteRequests_AspNetUsers_ClientId",
                table: "RouteRequests",
                column: "ClientId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
