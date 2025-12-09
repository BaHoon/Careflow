using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CareFlow.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddBarcodeImageFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ImageGeneratedAt",
                table: "BarcodeIndexes",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ImageMimeType",
                table: "BarcodeIndexes",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ImagePath",
                table: "BarcodeIndexes",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "ImageSize",
                table: "BarcodeIndexes",
                type: "bigint",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ImageGeneratedAt",
                table: "BarcodeIndexes");

            migrationBuilder.DropColumn(
                name: "ImageMimeType",
                table: "BarcodeIndexes");

            migrationBuilder.DropColumn(
                name: "ImagePath",
                table: "BarcodeIndexes");

            migrationBuilder.DropColumn(
                name: "ImageSize",
                table: "BarcodeIndexes");
        }
    }
}
