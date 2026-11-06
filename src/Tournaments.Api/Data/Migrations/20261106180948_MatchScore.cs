using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tournaments.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class MatchScore : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "ScoreA",
                table: "Matches",
                type: "double precision",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ScoreA",
                table: "Matches");
        }
    }
}
