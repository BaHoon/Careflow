using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CareFlow.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddInspectionOrderTaskSupport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "DrugId",
                table: "MedicationOrders",
                type: "character varying(50)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "ExecutionTasks",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "LastModifiedAt",
                table: "ExecutionTasks",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Drugs",
                columns: table => new
                {
                    Id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    GenericName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    TradeName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Manufacturer = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Specification = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    DosageForm = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    PackageSpec = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    AtcCode = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    Category = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    AdministrationRoute = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    UnitPrice = table.Column<decimal>(type: "numeric(10,4)", nullable: false),
                    PriceUnit = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    IsPrescriptionOnly = table.Column<bool>(type: "boolean", nullable: false),
                    IsNarcotic = table.Column<bool>(type: "boolean", nullable: false),
                    IsPsychotropic = table.Column<bool>(type: "boolean", nullable: false),
                    IsAntibiotic = table.Column<bool>(type: "boolean", nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Indications = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    Contraindications = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    DosageInstructions = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    SideEffects = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    StorageConditions = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    ShelfLifeMonths = table.Column<int>(type: "integer", nullable: true),
                    ApprovalNumber = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Remarks = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreateTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeleteTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Drugs", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MedicationOrders_DrugId",
                table: "MedicationOrders",
                column: "DrugId");

            migrationBuilder.AddForeignKey(
                name: "FK_MedicationOrders_Drugs_DrugId",
                table: "MedicationOrders",
                column: "DrugId",
                principalTable: "Drugs",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MedicationOrders_Drugs_DrugId",
                table: "MedicationOrders");

            migrationBuilder.DropTable(
                name: "Drugs");

            migrationBuilder.DropIndex(
                name: "IX_MedicationOrders_DrugId",
                table: "MedicationOrders");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "ExecutionTasks");

            migrationBuilder.DropColumn(
                name: "LastModifiedAt",
                table: "ExecutionTasks");

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
