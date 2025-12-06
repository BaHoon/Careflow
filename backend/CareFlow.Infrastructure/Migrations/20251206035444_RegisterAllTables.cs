using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace CareFlow.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RegisterAllTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Departments",
                columns: table => new
                {
                    DeptId = table.Column<string>(type: "text", nullable: false),
                    DeptName = table.Column<string>(type: "text", nullable: false),
                    Location = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Departments", x => x.DeptId);
                });

            migrationBuilder.CreateTable(
                name: "HospitalTimeSlots",
                columns: table => new
                {
                    SlotId = table.Column<int>(type: "integer", nullable: false),
                    SlotCode = table.Column<string>(type: "text", nullable: false),
                    SlotName = table.Column<string>(type: "text", nullable: false),
                    DefaultTime = table.Column<TimeSpan>(type: "interval", nullable: false),
                    OffsetMinutes = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HospitalTimeSlots", x => x.SlotId);
                });

            migrationBuilder.CreateTable(
                name: "Staffs",
                columns: table => new
                {
                    StaffId = table.Column<string>(type: "text", nullable: false),
                    PasswordHash = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    IdCard = table.Column<string>(type: "text", nullable: false),
                    Phone = table.Column<string>(type: "text", nullable: false),
                    RoleType = table.Column<string>(type: "text", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    DeptCode = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Staffs", x => x.StaffId);
                    table.ForeignKey(
                        name: "FK_Staffs_Departments_DeptCode",
                        column: x => x.DeptCode,
                        principalTable: "Departments",
                        principalColumn: "DeptId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Wards",
                columns: table => new
                {
                    WardId = table.Column<string>(type: "text", nullable: false),
                    DeptId = table.Column<string>(type: "text", nullable: false),
                    DepartmentDeptId = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Wards", x => x.WardId);
                    table.ForeignKey(
                        name: "FK_Wards_Departments_DepartmentDeptId",
                        column: x => x.DepartmentDeptId,
                        principalTable: "Departments",
                        principalColumn: "DeptId");
                });

            migrationBuilder.CreateTable(
                name: "Doctors",
                columns: table => new
                {
                    StaffId = table.Column<string>(type: "text", nullable: false),
                    Title = table.Column<string>(type: "text", nullable: false),
                    PrescriptionAuthLevel = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Doctors", x => x.StaffId);
                    table.ForeignKey(
                        name: "FK_Doctors_Staffs_StaffId",
                        column: x => x.StaffId,
                        principalTable: "Staffs",
                        principalColumn: "StaffId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Nurses",
                columns: table => new
                {
                    StaffId = table.Column<string>(type: "text", nullable: false),
                    NurseRank = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Nurses", x => x.StaffId);
                    table.ForeignKey(
                        name: "FK_Nurses_Staffs_StaffId",
                        column: x => x.StaffId,
                        principalTable: "Staffs",
                        principalColumn: "StaffId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Beds",
                columns: table => new
                {
                    BedId = table.Column<string>(type: "text", nullable: false),
                    WardId = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Beds", x => x.BedId);
                    table.ForeignKey(
                        name: "FK_Beds_Wards_WardId",
                        column: x => x.WardId,
                        principalTable: "Wards",
                        principalColumn: "WardId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Patients",
                columns: table => new
                {
                    PatientId = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Gender = table.Column<string>(type: "text", nullable: false),
                    IdCard = table.Column<string>(type: "text", nullable: false),
                    DateOfBirth = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Age = table.Column<int>(type: "integer", nullable: false),
                    Weight = table.Column<float>(type: "real", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    PhoneNumber = table.Column<string>(type: "text", nullable: false),
                    NursingGrade = table.Column<int>(type: "integer", nullable: false),
                    BedId = table.Column<string>(type: "text", nullable: false),
                    AttendingDoctorId = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Patients", x => x.PatientId);
                    table.ForeignKey(
                        name: "FK_Patients_Beds_BedId",
                        column: x => x.BedId,
                        principalTable: "Beds",
                        principalColumn: "BedId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Patients_Doctors_AttendingDoctorId",
                        column: x => x.AttendingDoctorId,
                        principalTable: "Doctors",
                        principalColumn: "StaffId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MedicalOrders",
                columns: table => new
                {
                    OrderId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PatientId = table.Column<string>(type: "text", nullable: false),
                    DoctorId = table.Column<string>(type: "text", nullable: false),
                    NurseId = table.Column<string>(type: "text", nullable: true),
                    CreateTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    PlantEndTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EndTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    OrderType = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    IsLongTerm = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MedicalOrders", x => x.OrderId);
                    table.ForeignKey(
                        name: "FK_MedicalOrders_Doctors_DoctorId",
                        column: x => x.DoctorId,
                        principalTable: "Doctors",
                        principalColumn: "StaffId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MedicalOrders_Nurses_NurseId",
                        column: x => x.NurseId,
                        principalTable: "Nurses",
                        principalColumn: "StaffId");
                    table.ForeignKey(
                        name: "FK_MedicalOrders_Patients_PatientId",
                        column: x => x.PatientId,
                        principalTable: "Patients",
                        principalColumn: "PatientId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "InspectionOrders",
                columns: table => new
                {
                    OrderId = table.Column<long>(type: "bigint", nullable: false),
                    ItemCode = table.Column<string>(type: "text", nullable: false),
                    RisLisId = table.Column<string>(type: "text", nullable: false),
                    Location = table.Column<string>(type: "text", nullable: false),
                    AppointmentTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    AppointmentPlace = table.Column<string>(type: "text", nullable: false),
                    Precautions = table.Column<string>(type: "text", nullable: false),
                    CheckStartTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CheckEndTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    BackToWardTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ReportTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ReportId = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InspectionOrders", x => x.OrderId);
                    table.ForeignKey(
                        name: "FK_InspectionOrders_MedicalOrders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "MedicalOrders",
                        principalColumn: "OrderId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MedicationOrders",
                columns: table => new
                {
                    OrderId = table.Column<long>(type: "bigint", nullable: false),
                    DrugId = table.Column<string>(type: "text", nullable: false),
                    Dosage = table.Column<string>(type: "text", nullable: false),
                    UsageRoute = table.Column<string>(type: "text", nullable: false),
                    IsDynamicUsage = table.Column<bool>(type: "boolean", nullable: false),
                    FreqCode = table.Column<string>(type: "text", nullable: false),
                    StartTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TimingStrategy = table.Column<string>(type: "text", nullable: false),
                    SpecificExecutionTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    SmartSlotsMask = table.Column<int>(type: "integer", nullable: false),
                    IntervalDays = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MedicationOrders", x => x.OrderId);
                    table.ForeignKey(
                        name: "FK_MedicationOrders_MedicalOrders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "MedicalOrders",
                        principalColumn: "OrderId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OperationOrders",
                columns: table => new
                {
                    OrderId = table.Column<long>(type: "bigint", nullable: false),
                    OpId = table.Column<string>(type: "text", nullable: false),
                    Normal = table.Column<bool>(type: "boolean", nullable: false),
                    FrequencyType = table.Column<string>(type: "text", nullable: false),
                    FrequencyValue = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OperationOrders", x => x.OrderId);
                    table.ForeignKey(
                        name: "FK_OperationOrders_MedicalOrders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "MedicalOrders",
                        principalColumn: "OrderId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SurgicalOrders",
                columns: table => new
                {
                    OrderId = table.Column<long>(type: "bigint", nullable: false),
                    SurgeryName = table.Column<string>(type: "text", nullable: false),
                    ScheduleTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    AnesthesiaType = table.Column<string>(type: "text", nullable: false),
                    IncisionSite = table.Column<string>(type: "text", nullable: false),
                    RequiredMeds = table.Column<string>(type: "jsonb", nullable: false),
                    NeedBloodPrep = table.Column<bool>(type: "boolean", nullable: false),
                    HasImplants = table.Column<bool>(type: "boolean", nullable: false),
                    PrepProgress = table.Column<float>(type: "real", nullable: false),
                    PrepStatus = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SurgicalOrders", x => x.OrderId);
                    table.ForeignKey(
                        name: "FK_SurgicalOrders_MedicalOrders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "MedicalOrders",
                        principalColumn: "OrderId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "InspectionReports",
                columns: table => new
                {
                    ReportId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    OrderId = table.Column<long>(type: "bigint", nullable: false),
                    PatientId = table.Column<string>(type: "text", nullable: false),
                    RisLisId = table.Column<string>(type: "text", nullable: false),
                    ReportTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ReportStatus = table.Column<string>(type: "text", nullable: false),
                    Findings = table.Column<string>(type: "text", nullable: false),
                    Impression = table.Column<string>(type: "text", nullable: false),
                    AttachmentUrl = table.Column<string>(type: "text", nullable: false),
                    ReviewerId = table.Column<string>(type: "text", nullable: false),
                    ReportSource = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InspectionReports", x => x.ReportId);
                    table.ForeignKey(
                        name: "FK_InspectionReports_InspectionOrders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "InspectionOrders",
                        principalColumn: "OrderId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_InspectionReports_Patients_PatientId",
                        column: x => x.PatientId,
                        principalTable: "Patients",
                        principalColumn: "PatientId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Beds_WardId",
                table: "Beds",
                column: "WardId");

            migrationBuilder.CreateIndex(
                name: "IX_InspectionReports_OrderId",
                table: "InspectionReports",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_InspectionReports_PatientId",
                table: "InspectionReports",
                column: "PatientId");

            migrationBuilder.CreateIndex(
                name: "IX_MedicalOrders_DoctorId",
                table: "MedicalOrders",
                column: "DoctorId");

            migrationBuilder.CreateIndex(
                name: "IX_MedicalOrders_NurseId",
                table: "MedicalOrders",
                column: "NurseId");

            migrationBuilder.CreateIndex(
                name: "IX_MedicalOrders_PatientId",
                table: "MedicalOrders",
                column: "PatientId");

            migrationBuilder.CreateIndex(
                name: "IX_Patients_AttendingDoctorId",
                table: "Patients",
                column: "AttendingDoctorId");

            migrationBuilder.CreateIndex(
                name: "IX_Patients_BedId",
                table: "Patients",
                column: "BedId");

            migrationBuilder.CreateIndex(
                name: "IX_Staffs_DeptCode",
                table: "Staffs",
                column: "DeptCode");

            migrationBuilder.CreateIndex(
                name: "IX_Wards_DepartmentDeptId",
                table: "Wards",
                column: "DepartmentDeptId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "HospitalTimeSlots");

            migrationBuilder.DropTable(
                name: "InspectionReports");

            migrationBuilder.DropTable(
                name: "MedicationOrders");

            migrationBuilder.DropTable(
                name: "OperationOrders");

            migrationBuilder.DropTable(
                name: "SurgicalOrders");

            migrationBuilder.DropTable(
                name: "InspectionOrders");

            migrationBuilder.DropTable(
                name: "MedicalOrders");

            migrationBuilder.DropTable(
                name: "Nurses");

            migrationBuilder.DropTable(
                name: "Patients");

            migrationBuilder.DropTable(
                name: "Beds");

            migrationBuilder.DropTable(
                name: "Doctors");

            migrationBuilder.DropTable(
                name: "Wards");

            migrationBuilder.DropTable(
                name: "Staffs");

            migrationBuilder.DropTable(
                name: "Departments");
        }
    }
}
