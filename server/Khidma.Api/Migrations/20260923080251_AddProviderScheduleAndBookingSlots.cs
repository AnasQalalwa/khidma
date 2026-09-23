using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Khidma.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddProviderScheduleAndBookingSlots : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<DateOnly>(
                name: "RequestedDate",
                table: "Bookings",
                type: "date",
                nullable: false,
                oldClrType: typeof(DateTimeOffset),
                oldType: "datetimeoffset");

            migrationBuilder.AddColumn<int>(
                name: "DurationHours",
                table: "Bookings",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RescheduleNote",
                table: "Bookings",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "RescheduledAt",
                table: "Bookings",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ScheduledStart",
                table: "Bookings",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ProviderWorkingHours",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProviderProfileId = table.Column<int>(type: "int", nullable: false),
                    DayOfWeek = table.Column<int>(type: "int", nullable: false),
                    Hour = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProviderWorkingHours", x => x.Id);
                    table.CheckConstraint("CK_ProviderWorkingHours_DayAndHour", "[DayOfWeek] >= 0 AND [DayOfWeek] <= 6 AND [Hour] >= 0 AND [Hour] <= 23");
                    table.ForeignKey(
                        name: "FK_ProviderWorkingHours_ProviderProfiles_ProviderProfileId",
                        column: x => x.ProviderProfileId,
                        principalTable: "ProviderProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.AddCheckConstraint(
                name: "CK_Bookings_DurationHours",
                table: "Bookings",
                sql: "[DurationHours] IS NULL OR ([DurationHours] >= 1 AND [DurationHours] <= 12)");

            migrationBuilder.CreateIndex(
                name: "IX_ProviderWorkingHours_ProviderProfileId_DayOfWeek_Hour",
                table: "ProviderWorkingHours",
                columns: new[] { "ProviderProfileId", "DayOfWeek", "Hour" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProviderWorkingHours");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Bookings_DurationHours",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "DurationHours",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "RescheduleNote",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "RescheduledAt",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "ScheduledStart",
                table: "Bookings");

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "RequestedDate",
                table: "Bookings",
                type: "datetimeoffset",
                nullable: false,
                oldClrType: typeof(DateOnly),
                oldType: "date");
        }
    }
}
