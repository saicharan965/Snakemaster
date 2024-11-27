using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SnakesMaster.API.Migrations
{
    /// <inheritdoc />
    public partial class cls : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_HighScores_Users_UserId",
                table: "HighScores");

            migrationBuilder.DropIndex(
                name: "IX_HighScores_UserId",
                table: "HighScores");

            migrationBuilder.AddColumn<Guid>(
                name: "ScoredBy",
                table: "HighScores",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ScoredBy",
                table: "HighScores");

            migrationBuilder.CreateIndex(
                name: "IX_HighScores_UserId",
                table: "HighScores",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_HighScores_Users_UserId",
                table: "HighScores",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "UserId",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
