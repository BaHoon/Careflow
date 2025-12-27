using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace CareFlow.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class hospital : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BarcodeIndexes",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    TableName = table.Column<string>(type: "text", nullable: false),
                    RecordId = table.Column<string>(type: "text", nullable: false),
                    ImagePath = table.Column<string>(type: "text", nullable: true),
                    ImageSize = table.Column<long>(type: "bigint", nullable: true),
                    ImageMimeType = table.Column<string>(type: "text", nullable: true),
                    ImageGeneratedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreateTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BarcodeIndexes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Departments",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    DeptName = table.Column<string>(type: "text", nullable: false),
                    Location = table.Column<string>(type: "text", nullable: false),
                    CreateTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Departments", x => x.Id);
                });

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

            migrationBuilder.CreateTable(
                name: "HospitalTimeSlots",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false),
                    SlotCode = table.Column<string>(type: "text", nullable: false),
                    SlotName = table.Column<string>(type: "text", nullable: false),
                    DefaultTime = table.Column<TimeSpan>(type: "interval", nullable: false),
                    OffsetMinutes = table.Column<int>(type: "integer", nullable: false),
                    CreateTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HospitalTimeSlots", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ShiftTypes",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    ShiftName = table.Column<string>(type: "text", nullable: false),
                    StartTime = table.Column<TimeSpan>(type: "interval", nullable: false),
                    EndTime = table.Column<TimeSpan>(type: "interval", nullable: false),
                    CreateTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShiftTypes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Staffs",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    EmployeeNumber = table.Column<string>(type: "text", nullable: false),
                    PasswordHash = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    IdCard = table.Column<string>(type: "text", nullable: false),
                    Phone = table.Column<string>(type: "text", nullable: false),
                    RoleType = table.Column<string>(type: "text", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    DeptCode = table.Column<string>(type: "text", nullable: false),
                    CreateTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Staffs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Staffs_Departments_DeptCode",
                        column: x => x.DeptCode,
                        principalTable: "Departments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Wards",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    DepartmentId = table.Column<string>(type: "text", nullable: false),
                    CreateTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Wards", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Wards_Departments_DepartmentId",
                        column: x => x.DepartmentId,
                        principalTable: "Departments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Doctors",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    Title = table.Column<string>(type: "text", nullable: false),
                    PrescriptionAuthLevel = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Doctors", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Doctors_Staffs_Id",
                        column: x => x.Id,
                        principalTable: "Staffs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Nurses",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    NurseRank = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Nurses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Nurses_Staffs_Id",
                        column: x => x.Id,
                        principalTable: "Staffs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Beds",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    WardId = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    CreateTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Beds", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Beds_Wards_WardId",
                        column: x => x.WardId,
                        principalTable: "Wards",
                        principalColumn: "Id",
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
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_NurseRosters_ShiftTypes_ShiftId",
                        column: x => x.ShiftId,
                        principalTable: "ShiftTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_NurseRosters_Wards_WardId",
                        column: x => x.WardId,
                        principalTable: "Wards",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Patients",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Gender = table.Column<string>(type: "text", nullable: false),
                    IdCard = table.Column<string>(type: "text", nullable: false),
                    DateOfBirth = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Age = table.Column<int>(type: "integer", nullable: false),
                    Weight = table.Column<float>(type: "real", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    PhoneNumber = table.Column<string>(type: "text", nullable: false),
                    NursingGrade = table.Column<int>(type: "integer", nullable: false),
                    BedId = table.Column<string>(type: "text", nullable: false),
                    AttendingDoctorId = table.Column<string>(type: "text", nullable: false),
                    CreateTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Patients", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Patients_Beds_BedId",
                        column: x => x.BedId,
                        principalTable: "Beds",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Patients_Doctors_AttendingDoctorId",
                        column: x => x.AttendingDoctorId,
                        principalTable: "Doctors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MedicalOrders",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PatientId = table.Column<string>(type: "text", nullable: false),
                    DoctorId = table.Column<string>(type: "text", nullable: false),
                    NurseId = table.Column<string>(type: "text", nullable: true),
                    PlantEndTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EndTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    OrderType = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    IsLongTerm = table.Column<bool>(type: "boolean", nullable: false),
                    Remarks = table.Column<string>(type: "text", nullable: true),
                    SignedByNurseId = table.Column<string>(type: "text", nullable: true),
                    SignedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RejectReason = table.Column<string>(type: "text", nullable: true),
                    RejectedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RejectedByNurseId = table.Column<string>(type: "text", nullable: true),
                    StopReason = table.Column<string>(type: "text", nullable: true),
                    StopOrderTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    StopDoctorId = table.Column<string>(type: "text", nullable: true),
                    StopConfirmedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    StopConfirmedByNurseId = table.Column<string>(type: "text", nullable: true),
                    StopRejectReason = table.Column<string>(type: "text", nullable: true),
                    StopRejectedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    StopRejectedByNurseId = table.Column<string>(type: "text", nullable: true),
                    CancelReason = table.Column<string>(type: "text", nullable: true),
                    CancelledAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CancelledByDoctorId = table.Column<string>(type: "text", nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CompletionType = table.Column<string>(type: "text", nullable: true),
                    ResubmittedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModificationNotes = table.Column<string>(type: "text", nullable: true),
                    CreateTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MedicalOrders", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MedicalOrders_Doctors_CancelledByDoctorId",
                        column: x => x.CancelledByDoctorId,
                        principalTable: "Doctors",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_MedicalOrders_Doctors_DoctorId",
                        column: x => x.DoctorId,
                        principalTable: "Doctors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MedicalOrders_Doctors_StopDoctorId",
                        column: x => x.StopDoctorId,
                        principalTable: "Doctors",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_MedicalOrders_Nurses_NurseId",
                        column: x => x.NurseId,
                        principalTable: "Nurses",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_MedicalOrders_Nurses_RejectedByNurseId",
                        column: x => x.RejectedByNurseId,
                        principalTable: "Nurses",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_MedicalOrders_Nurses_SignedByNurseId",
                        column: x => x.SignedByNurseId,
                        principalTable: "Nurses",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_MedicalOrders_Nurses_StopConfirmedByNurseId",
                        column: x => x.StopConfirmedByNurseId,
                        principalTable: "Nurses",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_MedicalOrders_Nurses_StopRejectedByNurseId",
                        column: x => x.StopRejectedByNurseId,
                        principalTable: "Nurses",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_MedicalOrders_Patients_PatientId",
                        column: x => x.PatientId,
                        principalTable: "Patients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "NursingCareNotes",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    NursingTaskId = table.Column<long>(type: "bigint", nullable: true),
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
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_NursingCareNotes_Patients_PatientId",
                        column: x => x.PatientId,
                        principalTable: "Patients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "NursingTasks",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PatientId = table.Column<string>(type: "text", nullable: false),
                    ScheduledTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    AssignedNurseId = table.Column<string>(type: "text", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    ExecuteTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ExecutorNurseId = table.Column<string>(type: "text", nullable: true),
                    CancelReason = table.Column<string>(type: "text", nullable: true),
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

            migrationBuilder.CreateTable(
                name: "VitalSignsRecords",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    NursingTaskId = table.Column<long>(type: "bigint", nullable: true),
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
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_VitalSignsRecords_Patients_PatientId",
                        column: x => x.PatientId,
                        principalTable: "Patients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DischargeOrders",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false),
                    DischargeType = table.Column<int>(type: "integer", nullable: false),
                    DischargeTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DischargeDiagnosis = table.Column<string>(type: "text", nullable: false),
                    DischargeInstructions = table.Column<string>(type: "text", nullable: false),
                    MedicationInstructions = table.Column<string>(type: "text", nullable: false),
                    RequiresFollowUp = table.Column<bool>(type: "boolean", nullable: false),
                    FollowUpDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DischargeConfirmedByNurseId = table.Column<string>(type: "text", nullable: true),
                    DischargeConfirmedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DischargeOrders", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DischargeOrders_MedicalOrders_Id",
                        column: x => x.Id,
                        principalTable: "MedicalOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ExecutionTasks",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    MedicalOrderId = table.Column<long>(type: "bigint", nullable: false),
                    PatientId = table.Column<string>(type: "text", nullable: false),
                    Category = table.Column<int>(type: "integer", nullable: false),
                    PlannedStartTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    AssignedNurseId = table.Column<string>(type: "text", nullable: true),
                    ActualStartTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ExecutorStaffId = table.Column<string>(type: "text", nullable: true),
                    ActualEndTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CompleterNurseId = table.Column<string>(type: "text", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    StatusBeforeLocking = table.Column<int>(type: "integer", nullable: true),
                    IsRolledBack = table.Column<bool>(type: "boolean", nullable: false),
                    DataPayload = table.Column<string>(type: "text", nullable: false),
                    ResultPayload = table.Column<string>(type: "text", nullable: true),
                    ExceptionReason = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreateTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExecutionTasks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ExecutionTasks_MedicalOrders_MedicalOrderId",
                        column: x => x.MedicalOrderId,
                        principalTable: "MedicalOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ExecutionTasks_Nurses_AssignedNurseId",
                        column: x => x.AssignedNurseId,
                        principalTable: "Nurses",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ExecutionTasks_Nurses_CompleterNurseId",
                        column: x => x.CompleterNurseId,
                        principalTable: "Nurses",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ExecutionTasks_Nurses_ExecutorStaffId",
                        column: x => x.ExecutorStaffId,
                        principalTable: "Nurses",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ExecutionTasks_Patients_PatientId",
                        column: x => x.PatientId,
                        principalTable: "Patients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "InspectionOrders",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false),
                    ItemCode = table.Column<string>(type: "text", nullable: false),
                    ItemName = table.Column<string>(type: "text", nullable: false),
                    RisLisId = table.Column<string>(type: "text", nullable: false),
                    Location = table.Column<string>(type: "text", nullable: false),
                    Source = table.Column<int>(type: "integer", nullable: false),
                    AppointmentTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    AppointmentPlace = table.Column<string>(type: "text", nullable: true),
                    Precautions = table.Column<string>(type: "text", nullable: true),
                    CheckStartTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CheckEndTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ReportPendingTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ReportTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ReportId = table.Column<string>(type: "text", nullable: true),
                    InspectionStatus = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InspectionOrders", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InspectionOrders_MedicalOrders_Id",
                        column: x => x.Id,
                        principalTable: "MedicalOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MedicalOrderStatusHistories",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    MedicalOrderId = table.Column<long>(type: "bigint", nullable: false),
                    FromStatus = table.Column<int>(type: "integer", nullable: false),
                    ToStatus = table.Column<int>(type: "integer", nullable: false),
                    ChangedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ChangedById = table.Column<string>(type: "text", nullable: false),
                    ChangedByType = table.Column<string>(type: "text", nullable: false),
                    ChangedByName = table.Column<string>(type: "text", nullable: true),
                    Reason = table.Column<string>(type: "text", nullable: true),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    CreateTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MedicalOrderStatusHistories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MedicalOrderStatusHistories_MedicalOrders_MedicalOrderId",
                        column: x => x.MedicalOrderId,
                        principalTable: "MedicalOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MedicationOrderItems",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    MedicalOrderId = table.Column<long>(type: "bigint", nullable: false),
                    DrugId = table.Column<string>(type: "character varying(50)", nullable: false),
                    Dosage = table.Column<string>(type: "text", nullable: false),
                    Note = table.Column<string>(type: "text", nullable: false),
                    CreateTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MedicationOrderItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MedicationOrderItems_Drugs_DrugId",
                        column: x => x.DrugId,
                        principalTable: "Drugs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MedicationOrderItems_MedicalOrders_MedicalOrderId",
                        column: x => x.MedicalOrderId,
                        principalTable: "MedicalOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MedicationOrders",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false),
                    UsageRoute = table.Column<int>(type: "integer", nullable: false),
                    IsDynamicUsage = table.Column<bool>(type: "boolean", nullable: false),
                    IntervalHours = table.Column<decimal>(type: "numeric", nullable: true),
                    StartTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TimingStrategy = table.Column<string>(type: "text", nullable: false),
                    SmartSlotsMask = table.Column<int>(type: "integer", nullable: false),
                    IntervalDays = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MedicationOrders", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MedicationOrders_MedicalOrders_Id",
                        column: x => x.Id,
                        principalTable: "MedicalOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OperationOrders",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false),
                    OpId = table.Column<string>(type: "text", nullable: false),
                    OperationName = table.Column<string>(type: "text", nullable: false),
                    Normal = table.Column<bool>(type: "boolean", nullable: false),
                    OperationSite = table.Column<string>(type: "text", nullable: true),
                    TimingStrategy = table.Column<string>(type: "text", nullable: false),
                    StartTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IntervalHours = table.Column<decimal>(type: "numeric", nullable: true),
                    SmartSlotsMask = table.Column<int>(type: "integer", nullable: false),
                    IntervalDays = table.Column<int>(type: "integer", nullable: false),
                    OperationRequirements = table.Column<string>(type: "text", nullable: true),
                    RequiresPreparation = table.Column<bool>(type: "boolean", nullable: false),
                    PreparationItems = table.Column<string>(type: "text", nullable: true),
                    ExpectedDurationMinutes = table.Column<int>(type: "integer", nullable: true),
                    RequiresResult = table.Column<bool>(type: "boolean", nullable: false),
                    ResultTemplate = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OperationOrders", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OperationOrders_MedicalOrders_Id",
                        column: x => x.Id,
                        principalTable: "MedicalOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SurgicalOrders",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false),
                    SurgeryName = table.Column<string>(type: "text", nullable: false),
                    ScheduleTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    AnesthesiaType = table.Column<string>(type: "text", nullable: false),
                    IncisionSite = table.Column<string>(type: "text", nullable: false),
                    SurgeonId = table.Column<string>(type: "text", nullable: false),
                    RequiredTalk = table.Column<string>(type: "text", nullable: true),
                    RequiredOperation = table.Column<string>(type: "text", nullable: true),
                    PrepProgress = table.Column<float>(type: "real", nullable: false),
                    PrepStatus = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SurgicalOrders", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SurgicalOrders_MedicalOrders_Id",
                        column: x => x.Id,
                        principalTable: "MedicalOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

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

            migrationBuilder.CreateTable(
                name: "MedicationReturnRequests",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ExecutionTaskId = table.Column<long>(type: "bigint", nullable: false),
                    ReturnType = table.Column<string>(type: "text", nullable: false),
                    RequestedBy = table.Column<string>(type: "text", nullable: false),
                    RequestedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Reason = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    SubmittedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ConfirmedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    PharmacyResponse = table.Column<string>(type: "text", nullable: true),
                    Remarks = table.Column<string>(type: "text", nullable: true),
                    CreateTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MedicationReturnRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MedicationReturnRequests_ExecutionTasks_ExecutionTaskId",
                        column: x => x.ExecutionTaskId,
                        principalTable: "ExecutionTasks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MedicationReturnRequests_Nurses_RequestedBy",
                        column: x => x.RequestedBy,
                        principalTable: "Nurses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "InspectionReports",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    OrderId = table.Column<long>(type: "bigint", nullable: false),
                    PatientId = table.Column<string>(type: "text", nullable: false),
                    RisLisId = table.Column<string>(type: "text", nullable: false),
                    ReportTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ReportStatus = table.Column<int>(type: "integer", nullable: false),
                    Findings = table.Column<string>(type: "text", nullable: true),
                    Impression = table.Column<string>(type: "text", nullable: true),
                    AttachmentUrl = table.Column<string>(type: "text", nullable: true),
                    ReviewerId = table.Column<string>(type: "text", nullable: true),
                    ReportSource = table.Column<int>(type: "integer", nullable: false),
                    CreateTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InspectionReports", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InspectionReports_Doctors_ReviewerId",
                        column: x => x.ReviewerId,
                        principalTable: "Doctors",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_InspectionReports_InspectionOrders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "InspectionOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_InspectionReports_Patients_PatientId",
                        column: x => x.PatientId,
                        principalTable: "Patients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Beds_WardId",
                table: "Beds",
                column: "WardId");

            migrationBuilder.CreateIndex(
                name: "IX_ExecutionTasks_AssignedNurseId",
                table: "ExecutionTasks",
                column: "AssignedNurseId");

            migrationBuilder.CreateIndex(
                name: "IX_ExecutionTasks_CompleterNurseId",
                table: "ExecutionTasks",
                column: "CompleterNurseId");

            migrationBuilder.CreateIndex(
                name: "IX_ExecutionTasks_ExecutorStaffId",
                table: "ExecutionTasks",
                column: "ExecutorStaffId");

            migrationBuilder.CreateIndex(
                name: "IX_ExecutionTasks_MedicalOrderId",
                table: "ExecutionTasks",
                column: "MedicalOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_ExecutionTasks_PatientId",
                table: "ExecutionTasks",
                column: "PatientId");

            migrationBuilder.CreateIndex(
                name: "IX_InspectionReports_OrderId",
                table: "InspectionReports",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_InspectionReports_PatientId",
                table: "InspectionReports",
                column: "PatientId");

            migrationBuilder.CreateIndex(
                name: "IX_InspectionReports_ReviewerId",
                table: "InspectionReports",
                column: "ReviewerId");

            migrationBuilder.CreateIndex(
                name: "IX_MedicalOrders_CancelledByDoctorId",
                table: "MedicalOrders",
                column: "CancelledByDoctorId");

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
                name: "IX_MedicalOrders_RejectedByNurseId",
                table: "MedicalOrders",
                column: "RejectedByNurseId");

            migrationBuilder.CreateIndex(
                name: "IX_MedicalOrders_SignedByNurseId",
                table: "MedicalOrders",
                column: "SignedByNurseId");

            migrationBuilder.CreateIndex(
                name: "IX_MedicalOrders_StopConfirmedByNurseId",
                table: "MedicalOrders",
                column: "StopConfirmedByNurseId");

            migrationBuilder.CreateIndex(
                name: "IX_MedicalOrders_StopDoctorId",
                table: "MedicalOrders",
                column: "StopDoctorId");

            migrationBuilder.CreateIndex(
                name: "IX_MedicalOrders_StopRejectedByNurseId",
                table: "MedicalOrders",
                column: "StopRejectedByNurseId");

            migrationBuilder.CreateIndex(
                name: "IX_MedicalOrderStatusHistories_MedicalOrderId",
                table: "MedicalOrderStatusHistories",
                column: "MedicalOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_MedicationOrderItems_DrugId",
                table: "MedicationOrderItems",
                column: "DrugId");

            migrationBuilder.CreateIndex(
                name: "IX_MedicationOrderItems_MedicalOrderId",
                table: "MedicationOrderItems",
                column: "MedicalOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_MedicationReturnRequests_ExecutionTaskId",
                table: "MedicationReturnRequests",
                column: "ExecutionTaskId");

            migrationBuilder.CreateIndex(
                name: "IX_MedicationReturnRequests_RequestedBy",
                table: "MedicationReturnRequests",
                column: "RequestedBy");

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
                name: "IX_NursingRecordSupplements_NursingTaskId",
                table: "NursingRecordSupplements",
                column: "NursingTaskId");

            migrationBuilder.CreateIndex(
                name: "IX_NursingRecordSupplements_SupplementNurseId",
                table: "NursingRecordSupplements",
                column: "SupplementNurseId");

            migrationBuilder.CreateIndex(
                name: "IX_NursingTasks_AssignedNurseId",
                table: "NursingTasks",
                column: "AssignedNurseId");

            migrationBuilder.CreateIndex(
                name: "IX_NursingTasks_PatientId",
                table: "NursingTasks",
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
                name: "IX_VitalSignsRecords_PatientId",
                table: "VitalSignsRecords",
                column: "PatientId");

            migrationBuilder.CreateIndex(
                name: "IX_VitalSignsRecords_RecorderNurseId",
                table: "VitalSignsRecords",
                column: "RecorderNurseId");

            migrationBuilder.CreateIndex(
                name: "IX_Wards_DepartmentId",
                table: "Wards",
                column: "DepartmentId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BarcodeIndexes");

            migrationBuilder.DropTable(
                name: "DischargeOrders");

            migrationBuilder.DropTable(
                name: "HospitalTimeSlots");

            migrationBuilder.DropTable(
                name: "InspectionReports");

            migrationBuilder.DropTable(
                name: "MedicalOrderStatusHistories");

            migrationBuilder.DropTable(
                name: "MedicationOrderItems");

            migrationBuilder.DropTable(
                name: "MedicationOrders");

            migrationBuilder.DropTable(
                name: "MedicationReturnRequests");

            migrationBuilder.DropTable(
                name: "NurseRosters");

            migrationBuilder.DropTable(
                name: "NursingCareNotes");

            migrationBuilder.DropTable(
                name: "NursingRecordSupplements");

            migrationBuilder.DropTable(
                name: "OperationOrders");

            migrationBuilder.DropTable(
                name: "SurgicalOrders");

            migrationBuilder.DropTable(
                name: "VitalSignsRecords");

            migrationBuilder.DropTable(
                name: "InspectionOrders");

            migrationBuilder.DropTable(
                name: "Drugs");

            migrationBuilder.DropTable(
                name: "ExecutionTasks");

            migrationBuilder.DropTable(
                name: "ShiftTypes");

            migrationBuilder.DropTable(
                name: "NursingTasks");

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
