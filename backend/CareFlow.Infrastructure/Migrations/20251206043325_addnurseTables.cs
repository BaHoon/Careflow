using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace CareFlow.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class addnurseTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ExecutionTasks",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    OrderId = table.Column<long>(type: "bigint", nullable: false),
                    PatientId = table.Column<string>(type: "text", nullable: false),
                    PlannedStartTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ActualStartTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ActualEndTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ExecutorStaffId = table.Column<string>(type: "text", nullable: true),
                    Status = table.Column<string>(type: "text", nullable: false),
                    DataPayload = table.Column<string>(type: "text", nullable: false),
                    ExceptionReason = table.Column<string>(type: "text", nullable: false),
                    CreateTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExecutionTasks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ExecutionTasks_MedicalOrders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "MedicalOrders",
                        principalColumn: "OrderId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ExecutionTasks_Nurses_ExecutorStaffId",
                        column: x => x.ExecutorStaffId,
                        principalTable: "Nurses",
                        principalColumn: "StaffId");
                    table.ForeignKey(
                        name: "FK_ExecutionTasks_Patients_PatientId",
                        column: x => x.PatientId,
                        principalTable: "Patients",
                        principalColumn: "PatientId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "NursingCareNotes",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PatientId = table.Column<string>(type: "text", nullable: false),
                    RecorderNurseId = table.Column<string>(type: "text", nullable: false),
                    RecordTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Consciousness = table.Column<string>(type: "text", nullable: false),
                    PupilLeft = table.Column<string>(type: "text", nullable: false),
                    PupilRight = table.Column<string>(type: "text", nullable: false),
                    SkinCondition = table.Column<string>(type: "text", nullable: false),
                    PipeCareData = table.Column<string>(type: "jsonb", nullable: false),
                    IntakeVolume = table.Column<decimal>(type: "numeric", nullable: false),
                    IntakeType = table.Column<string>(type: "text", nullable: false),
                    OutputVolume = table.Column<decimal>(type: "numeric", nullable: false),
                    OutputType = table.Column<string>(type: "text", nullable: false),
                    Content = table.Column<string>(type: "text", nullable: false),
                    HealthEducation = table.Column<string>(type: "text", nullable: false),
                    IsAmended = table.Column<bool>(type: "boolean", nullable: false),
                    ParentAmendId = table.Column<long>(type: "bigint", nullable: true),
                    CreateTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NursingCareNotes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NursingCareNotes_Nurses_RecorderNurseId",
                        column: x => x.RecorderNurseId,
                        principalTable: "Nurses",
                        principalColumn: "StaffId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_NursingCareNotes_Patients_PatientId",
                        column: x => x.PatientId,
                        principalTable: "Patients",
                        principalColumn: "PatientId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ShiftTypes",
                columns: table => new
                {
                    ShiftId = table.Column<string>(type: "text", nullable: false),
                    ShiftName = table.Column<string>(type: "text", nullable: false),
                    StartTime = table.Column<TimeSpan>(type: "interval", nullable: false),
                    EndTime = table.Column<TimeSpan>(type: "interval", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShiftTypes", x => x.ShiftId);
                });

            migrationBuilder.CreateTable(
                name: "VitalSignsRecords",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PatientId = table.Column<string>(type: "text", nullable: false),
                    RecorderNurseId = table.Column<string>(type: "text", nullable: false),
                    RecordTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Temperature = table.Column<decimal>(type: "numeric", nullable: false),
                    TempType = table.Column<string>(type: "text", nullable: false),
                    Pulse = table.Column<int>(type: "integer", nullable: false),
                    Respiration = table.Column<int>(type: "integer", nullable: false),
                    SysBp = table.Column<int>(type: "integer", nullable: false),
                    DiaBp = table.Column<int>(type: "integer", nullable: false),
                    Spo2 = table.Column<decimal>(type: "numeric", nullable: false),
                    PainScore = table.Column<int>(type: "integer", nullable: false),
                    Weight = table.Column<decimal>(type: "numeric", nullable: false),
                    Intervention = table.Column<string>(type: "text", nullable: false),
                    CreateTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VitalSignsRecords", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VitalSignsRecords_Nurses_RecorderNurseId",
                        column: x => x.RecorderNurseId,
                        principalTable: "Nurses",
                        principalColumn: "StaffId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_VitalSignsRecords_Patients_PatientId",
                        column: x => x.PatientId,
                        principalTable: "Patients",
                        principalColumn: "PatientId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "NurseRosters",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    StaffId = table.Column<string>(type: "text", nullable: false),
                    WardId = table.Column<string>(type: "text", nullable: false),
                    ShiftId = table.Column<string>(type: "text", nullable: false),
                    WorkDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    CreateTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NurseRosters", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NurseRosters_Nurses_StaffId",
                        column: x => x.StaffId,
                        principalTable: "Nurses",
                        principalColumn: "StaffId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_NurseRosters_ShiftTypes_ShiftId",
                        column: x => x.ShiftId,
                        principalTable: "ShiftTypes",
                        principalColumn: "ShiftId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_NurseRosters_Wards_WardId",
                        column: x => x.WardId,
                        principalTable: "Wards",
                        principalColumn: "WardId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ExecutionTasks_ExecutorStaffId",
                table: "ExecutionTasks",
                column: "ExecutorStaffId");

            migrationBuilder.CreateIndex(
                name: "IX_ExecutionTasks_OrderId",
                table: "ExecutionTasks",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_ExecutionTasks_PatientId",
                table: "ExecutionTasks",
                column: "PatientId");

            migrationBuilder.CreateIndex(
                name: "IX_NurseRosters_ShiftId",
                table: "NurseRosters",
                column: "ShiftId");

            migrationBuilder.CreateIndex(
                name: "IX_NurseRosters_StaffId",
                table: "NurseRosters",
                column: "StaffId");

            migrationBuilder.CreateIndex(
                name: "IX_NurseRosters_WardId",
                table: "NurseRosters",
                column: "WardId");

            migrationBuilder.CreateIndex(
                name: "IX_NursingCareNotes_PatientId",
                table: "NursingCareNotes",
                column: "PatientId");

            migrationBuilder.CreateIndex(
                name: "IX_NursingCareNotes_RecorderNurseId",
                table: "NursingCareNotes",
                column: "RecorderNurseId");

            migrationBuilder.CreateIndex(
                name: "IX_VitalSignsRecords_PatientId",
                table: "VitalSignsRecords",
                column: "PatientId");

            migrationBuilder.CreateIndex(
                name: "IX_VitalSignsRecords_RecorderNurseId",
                table: "VitalSignsRecords",
                column: "RecorderNurseId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ExecutionTasks");

            migrationBuilder.DropTable(
                name: "NurseRosters");

            migrationBuilder.DropTable(
                name: "NursingCareNotes");

            migrationBuilder.DropTable(
                name: "VitalSignsRecords");

            migrationBuilder.DropTable(
                name: "ShiftTypes");
        }
    }
}
