using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Scrabble.Server.Migrations
{
    /// <inheritdoc />
    public partial class CascadeDelete : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Chats_Games",
                table: "Chats");

            migrationBuilder.DropForeignKey(
                name: "FK_Chats_Players",
                table: "Chats");

            migrationBuilder.DropForeignKey(
                name: "FK_PlayerGames_Games",
                table: "PlayerGames");

            migrationBuilder.DropForeignKey(
                name: "FK_PlayerGames_Players",
                table: "PlayerGames");

            migrationBuilder.AddForeignKey(
                name: "FK_Chats_Games",
                table: "Chats",
                column: "GameId",
                principalTable: "Games",
                principalColumn: "GameId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Chats_Players",
                table: "Chats",
                column: "PlayerId",
                principalTable: "Players",
                principalColumn: "PlayerId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_PlayerGames_Games",
                table: "PlayerGames",
                column: "GameId",
                principalTable: "Games",
                principalColumn: "GameId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_PlayerGames_Players",
                table: "PlayerGames",
                column: "PlayerId",
                principalTable: "Players",
                principalColumn: "PlayerId",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Chats_Games",
                table: "Chats");

            migrationBuilder.DropForeignKey(
                name: "FK_Chats_Players",
                table: "Chats");

            migrationBuilder.DropForeignKey(
                name: "FK_PlayerGames_Games",
                table: "PlayerGames");

            migrationBuilder.DropForeignKey(
                name: "FK_PlayerGames_Players",
                table: "PlayerGames");

            migrationBuilder.AddForeignKey(
                name: "FK_Chats_Games",
                table: "Chats",
                column: "GameId",
                principalTable: "Games",
                principalColumn: "GameId");

            migrationBuilder.AddForeignKey(
                name: "FK_Chats_Players",
                table: "Chats",
                column: "PlayerId",
                principalTable: "Players",
                principalColumn: "PlayerId");

            migrationBuilder.AddForeignKey(
                name: "FK_PlayerGames_Games",
                table: "PlayerGames",
                column: "GameId",
                principalTable: "Games",
                principalColumn: "GameId");

            migrationBuilder.AddForeignKey(
                name: "FK_PlayerGames_Players",
                table: "PlayerGames",
                column: "PlayerId",
                principalTable: "Players",
                principalColumn: "PlayerId");
        }
    }
}
