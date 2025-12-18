using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CareFlow.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdateSomething : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "NursingTaskId",
                table: "VitalSignsRecords",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "NursingTaskId",
                table: "NursingCareNotes",
                type: "bigint",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "NursingTaskId",
                table: "VitalSignsRecords");

            migrationBuilder.DropColumn(
                name: "NursingTaskId",
                table: "NursingCareNotes");
        }
    }
}
