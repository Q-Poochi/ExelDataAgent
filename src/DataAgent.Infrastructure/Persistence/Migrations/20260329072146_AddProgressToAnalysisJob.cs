using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAgent.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddProgressToAnalysisJob : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Progress",
                table: "AnalysisJobs",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Progress",
                table: "AnalysisJobs");
        }
    }
}
