using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartHomeSystem.Migrations
{
    /// <inheritdoc />
    public partial class repeatAlarm : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "RepeatDaily",
                table: "Alarms",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RepeatDaily",
                table: "Alarms");
        }
    }
}
