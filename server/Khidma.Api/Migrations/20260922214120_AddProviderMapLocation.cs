using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Khidma.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddProviderMapLocation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "Latitude",
                table: "ProviderProfiles",
                type: "decimal(9,6)",
                precision: 9,
                scale: 6,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Longitude",
                table: "ProviderProfiles",
                type: "decimal(9,6)",
                precision: 9,
                scale: 6,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Latitude",
                table: "ProviderProfiles");

            migrationBuilder.DropColumn(
                name: "Longitude",
                table: "ProviderProfiles");
        }
    }
}
