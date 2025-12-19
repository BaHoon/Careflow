using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace CareFlow.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddNursingTaskTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "NursingTasks",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PatientId = table.Column<string>(type: "text", nullable: false),
                    ScheduledTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    AssignedNurseId = table.Column<string>(type: "text", nullable: true),
                    Status = table.Column<string>(type: "text", nullable: false),
                    ExecuteTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ExecutorNurseId = table.Column<string>(type: "text", nullable: true),
                    TaskType = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    CreateTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NursingTasks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NursingTasks_Nurses_AssignedNurseId",
                        column: x => x.AssignedNurseId,
                        principalTable: "Nurses",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_NursingTasks_Patients_PatientId",
                        column: x => x.PatientId,
                        principalTable: "Patients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_NursingTasks_AssignedNurseId",
                table: "NursingTasks",
                column: "AssignedNurseId");

            migrationBuilder.CreateIndex(
                name: "IX_NursingTasks_PatientId",
                table: "NursingTasks",
                column: "PatientId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "NursingTasks");
        }
    }
}
