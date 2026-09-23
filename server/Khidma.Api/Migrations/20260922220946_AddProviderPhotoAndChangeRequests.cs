using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Khidma.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddProviderPhotoAndChangeRequests : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ServiceId",
                table: "ProviderVerificationDocuments",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PhotoContentType",
                table: "ProviderProfiles",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PhotoStoredFileName",
                table: "ProviderProfiles",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ProviderProfileChangeRequests",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProviderProfileId = table.Column<int>(type: "int", nullable: false),
                    Type = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    RequestedCity = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    RequestedLatitude = table.Column<decimal>(type: "decimal(9,6)", precision: 9, scale: 6, nullable: true),
                    RequestedLongitude = table.Column<decimal>(type: "decimal(9,6)", precision: 9, scale: 6, nullable: true),
                    ServiceId = table.Column<int>(type: "int", nullable: true),
                    ProofDocumentId = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ReviewedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ReviewedByUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    ReviewNote = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProviderProfileChangeRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProviderProfileChangeRequests_ProviderProfiles_ProviderProfileId",
                        column: x => x.ProviderProfileId,
                        principalTable: "ProviderProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProviderProfileChangeRequests_ProviderVerificationDocuments_ProofDocumentId",
                        column: x => x.ProofDocumentId,
                        principalTable: "ProviderVerificationDocuments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProviderProfileChangeRequests_Services_ServiceId",
                        column: x => x.ServiceId,
                        principalTable: "Services",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProviderVerificationDocuments_ServiceId",
                table: "ProviderVerificationDocuments",
                column: "ServiceId");

            migrationBuilder.CreateIndex(
                name: "IX_ProviderProfileChangeRequests_ProofDocumentId",
                table: "ProviderProfileChangeRequests",
                column: "ProofDocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_ProviderProfileChangeRequests_ServiceId",
                table: "ProviderProfileChangeRequests",
                column: "ServiceId");

            migrationBuilder.CreateIndex(
                name: "UX_ProviderChange_PendingLocation",
                table: "ProviderProfileChangeRequests",
                column: "ProviderProfileId",
                unique: true,
                filter: "[Status] = 'Pending' AND [Type] = 'Location'");

            migrationBuilder.CreateIndex(
                name: "UX_ProviderChange_PendingService",
                table: "ProviderProfileChangeRequests",
                columns: new[] { "ProviderProfileId", "ServiceId" },
                unique: true,
                filter: "[Status] = 'Pending' AND [Type] = 'AddService' AND [ServiceId] IS NOT NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_ProviderVerificationDocuments_Services_ServiceId",
                table: "ProviderVerificationDocuments",
                column: "ServiceId",
                principalTable: "Services",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ProviderVerificationDocuments_Services_ServiceId",
                table: "ProviderVerificationDocuments");

            migrationBuilder.DropTable(
                name: "ProviderProfileChangeRequests");

            migrationBuilder.DropIndex(
                name: "IX_ProviderVerificationDocuments_ServiceId",
                table: "ProviderVerificationDocuments");

            migrationBuilder.DropColumn(
                name: "ServiceId",
                table: "ProviderVerificationDocuments");

            migrationBuilder.DropColumn(
                name: "PhotoContentType",
                table: "ProviderProfiles");

            migrationBuilder.DropColumn(
                name: "PhotoStoredFileName",
                table: "ProviderProfiles");
        }
    }
}
