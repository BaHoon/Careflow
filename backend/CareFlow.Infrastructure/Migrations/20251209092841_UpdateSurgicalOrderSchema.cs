using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CareFlow.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdateSurgicalOrderSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "HasImplants",
                table: "SurgicalOrders");

            migrationBuilder.DropColumn(
                name: "NeedBloodPrep",
                table: "SurgicalOrders");

            migrationBuilder.AlterColumn<string>(
                name: "RequiredMeds",
                table: "SurgicalOrders",
                type: "jsonb",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "jsonb");

            migrationBuilder.AddColumn<string>(
                name: "RequiredOperation",
                table: "SurgicalOrders",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RequiredTalk",
                table: "SurgicalOrders",
                type: "text",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "DrugId",
                table: "MedicationOrders",
                type: "character varying(50)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RequiredOperation",
                table: "SurgicalOrders");

            migrationBuilder.DropColumn(
                name: "RequiredTalk",
                table: "SurgicalOrders");

            migrationBuilder.AlterColumn<string>(
                name: "RequiredMeds",
                table: "SurgicalOrders",
                type: "jsonb",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "jsonb",
                oldNullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "HasImplants",
                table: "SurgicalOrders",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "NeedBloodPrep",
                table: "SurgicalOrders",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AlterColumn<string>(
                name: "DrugId",
                table: "MedicationOrders",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(50)");
        }
    }
}
