using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace CareFlow.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddNursingRecordSupplementTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CancelReason",
                table: "NursingTasks",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AssignedNurseId",
                table: "ExecutionTasks",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "StatusBeforeLocking",
                table: "ExecutionTasks",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "NursingRecordSupplements",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    NursingTaskId = table.Column<long>(type: "bigint", nullable: false),
                    SupplementNurseId = table.Column<string>(type: "text", nullable: false),
                    SupplementTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Content = table.Column<string>(type: "text", nullable: false),
                    SupplementType = table.Column<string>(type: "text", nullable: false),
                    CreateTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NursingRecordSupplements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NursingRecordSupplements_Nurses_SupplementNurseId",
                        column: x => x.SupplementNurseId,
                        principalTable: "Nurses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_NursingRecordSupplements_NursingTasks_NursingTaskId",
                        column: x => x.NursingTaskId,
                        principalTable: "NursingTasks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ExecutionTasks_AssignedNurseId",
                table: "ExecutionTasks",
                column: "AssignedNurseId");

            migrationBuilder.CreateIndex(
                name: "IX_NursingRecordSupplements_NursingTaskId",
                table: "NursingRecordSupplements",
                column: "NursingTaskId");

            migrationBuilder.CreateIndex(
                name: "IX_NursingRecordSupplements_SupplementNurseId",
                table: "NursingRecordSupplements",
                column: "SupplementNurseId");

            migrationBuilder.AddForeignKey(
                name: "FK_ExecutionTasks_Nurses_AssignedNurseId",
                table: "ExecutionTasks",
                column: "AssignedNurseId",
                principalTable: "Nurses",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ExecutionTasks_Nurses_AssignedNurseId",
                table: "ExecutionTasks");

            migrationBuilder.DropTable(
                name: "NursingRecordSupplements");

            migrationBuilder.DropIndex(
                name: "IX_ExecutionTasks_AssignedNurseId",
                table: "ExecutionTasks");

            migrationBuilder.DropColumn(
                name: "CancelReason",
                table: "NursingTasks");

            migrationBuilder.DropColumn(
                name: "AssignedNurseId",
                table: "ExecutionTasks");

            migrationBuilder.DropColumn(
                name: "StatusBeforeLocking",
                table: "ExecutionTasks");
        }
    }
}
