using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CareFlow.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddExecutionTaskNewProperties : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Category",
                table: "ExecutionTasks",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "CompleterNurseId",
                table: "ExecutionTasks",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ResultPayload",
                table: "ExecutionTasks",
                type: "text",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ExecutionTasks_CompleterNurseId",
                table: "ExecutionTasks",
                column: "CompleterNurseId");

            migrationBuilder.AddForeignKey(
                name: "FK_ExecutionTasks_Nurses_CompleterNurseId",
                table: "ExecutionTasks",
                column: "CompleterNurseId",
                principalTable: "Nurses",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ExecutionTasks_Nurses_CompleterNurseId",
                table: "ExecutionTasks");

            migrationBuilder.DropIndex(
                name: "IX_ExecutionTasks_CompleterNurseId",
                table: "ExecutionTasks");

            migrationBuilder.DropColumn(
                name: "Category",
                table: "ExecutionTasks");

            migrationBuilder.DropColumn(
                name: "CompleterNurseId",
                table: "ExecutionTasks");

            migrationBuilder.DropColumn(
                name: "ResultPayload",
                table: "ExecutionTasks");
        }
    }
}
