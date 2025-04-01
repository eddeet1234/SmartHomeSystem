using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartHomeSystem.Migrations
{
    /// <inheritdoc />
    public partial class UpdateLightScheduleToTimeOnly : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Executed",
                table: "LightSchedules");

            migrationBuilder.AlterColumn<TimeSpan>(
                name: "OnTime",
                table: "LightSchedules",
                type: "time",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.AlterColumn<TimeSpan>(
                name: "OffTime",
                table: "LightSchedules",
                type: "time",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<DateTime>(
                name: "OnTime",
                table: "LightSchedules",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(TimeSpan),
                oldType: "time");

            migrationBuilder.AlterColumn<DateTime>(
                name: "OffTime",
                table: "LightSchedules",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(TimeSpan),
                oldType: "time",
                oldNullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "Executed",
                table: "LightSchedules",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }
    }
}
