using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CareFlow.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RemoveDrugIdFromMedicationOrder : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MedicationOrders_Drugs_DrugId",
                table: "MedicationOrders");

            migrationBuilder.DropIndex(
                name: "IX_MedicationOrders_DrugId",
                table: "MedicationOrders");

            migrationBuilder.DropColumn(
                name: "DrugId",
                table: "MedicationOrders");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DrugId",
                table: "MedicationOrders",
                type: "character varying(50)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_MedicationOrders_DrugId",
                table: "MedicationOrders",
                column: "DrugId");

            migrationBuilder.AddForeignKey(
                name: "FK_MedicationOrders_Drugs_DrugId",
                table: "MedicationOrders",
                column: "DrugId",
                principalTable: "Drugs",
                principalColumn: "Id");
        }
    }
}
