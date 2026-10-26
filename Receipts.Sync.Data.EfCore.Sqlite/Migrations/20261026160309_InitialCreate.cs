using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Receipts.Sync.Data.EfCore.Sqlite.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FileProviders",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    ProviderType = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    Configuration = table.Column<string>(type: "TEXT", nullable: false),
                    IsEnabled = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: true),
                    Description = table.Column<string>(type: "TEXT", maxLength: 1024, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FileProviders", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ExpenseFileReceipts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    FileProviderId = table.Column<int>(type: "INTEGER", nullable: false),
                    ExternalId = table.Column<string>(type: "TEXT", maxLength: 1024, nullable: false),
                    Hash = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    HashAlgorithm = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false, defaultValue: "SHA256"),
                    Url = table.Column<string>(type: "TEXT", maxLength: 2048, nullable: true),
                    FileName = table.Column<string>(type: "TEXT", maxLength: 512, nullable: true),
                    ContentType = table.Column<string>(type: "TEXT", maxLength: 128, nullable: true),
                    SizeInBytes = table.Column<long>(type: "INTEGER", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExpenseFileReceipts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ExpenseFileReceipts_FileProviders_FileProviderId",
                        column: x => x.FileProviderId,
                        principalTable: "FileProviders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ExpenseReceiptExtractions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ExpenseFileReceiptId = table.Column<int>(type: "INTEGER", nullable: false),
                    Metadata = table.Column<string>(type: "TEXT", nullable: false),
                    RawResponse = table.Column<string>(type: "TEXT", nullable: true),
                    ExtractorName = table.Column<string>(type: "TEXT", maxLength: 128, nullable: true),
                    ExtractedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExpenseReceiptExtractions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ExpenseReceiptExtractions_ExpenseFileReceipts_ExpenseFileReceiptId",
                        column: x => x.ExpenseFileReceiptId,
                        principalTable: "ExpenseFileReceipts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ExpenseFileReceipts_FileProviderId_ExternalId",
                table: "ExpenseFileReceipts",
                columns: new[] { "FileProviderId", "ExternalId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ExpenseFileReceipts_Hash",
                table: "ExpenseFileReceipts",
                column: "Hash");

            migrationBuilder.CreateIndex(
                name: "IX_ExpenseReceiptExtractions_ExpenseFileReceiptId",
                table: "ExpenseReceiptExtractions",
                column: "ExpenseFileReceiptId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FileProviders_Name",
                table: "FileProviders",
                column: "Name",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ExpenseReceiptExtractions");

            migrationBuilder.DropTable(
                name: "ExpenseFileReceipts");

            migrationBuilder.DropTable(
                name: "FileProviders");
        }
    }
}
