using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LivePulse.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddMatchSeasonOrder : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "SeasonOrder",
                table: "Matches",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SeasonOrder",
                table: "Matches");
        }
    }
}
