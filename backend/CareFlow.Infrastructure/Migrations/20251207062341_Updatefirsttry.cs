using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace CareFlow.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Updatefirsttry : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ExecutionTasks_MedicalOrders_OrderId",
                table: "ExecutionTasks");

            migrationBuilder.DropForeignKey(
                name: "FK_Wards_Departments_DepartmentDeptId",
                table: "Wards");

            migrationBuilder.DropIndex(
                name: "IX_Wards_DepartmentDeptId",
                table: "Wards");

            migrationBuilder.DropColumn(
                name: "DepartmentDeptId",
                table: "Wards");

            migrationBuilder.DropColumn(
                name: "OrderId",
                table: "MedicalOrders");

            migrationBuilder.DropColumn(
                name: "SlotId",
                table: "HospitalTimeSlots");

            migrationBuilder.RenameColumn(
                name: "DeptId",
                table: "Wards",
                newName: "DepartmentId");

            migrationBuilder.RenameColumn(
                name: "WardId",
                table: "Wards",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "ShiftId",
                table: "ShiftTypes",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "PatientId",
                table: "Patients",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "ReportId",
                table: "InspectionReports",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "OrderId",
                table: "ExecutionTasks",
                newName: "MedicalOrderId");

            migrationBuilder.RenameIndex(
                name: "IX_ExecutionTasks_OrderId",
                table: "ExecutionTasks",
                newName: "IX_ExecutionTasks_MedicalOrderId");

            migrationBuilder.RenameColumn(
                name: "DeptId",
                table: "Departments",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "BedId",
                table: "Beds",
                newName: "Id");

            migrationBuilder.AddColumn<DateTime>(
                name: "CreateTime",
                table: "Wards",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "CreateTime",
                table: "ShiftTypes",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "CreateTime",
                table: "Patients",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AlterColumn<string>(
                name: "ReviewerId",
                table: "InspectionReports",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");
    // 手动处理 text 到 integer 的转换
            migrationBuilder.Sql(@"
                ALTER TABLE ""InspectionReports"" 
                ALTER COLUMN ""ReportStatus"" TYPE integer 
                USING (CASE 
                    WHEN ""ReportStatus"" = '1' OR ""ReportStatus"" = 'Pending' THEN 1
                    WHEN ""ReportStatus"" = '2' OR ""ReportStatus"" = 'Completed' THEN 2
                    ELSE 1
                END)::integer;
            ");

          migrationBuilder.Sql(@"
                ALTER TABLE ""InspectionReports"" 
                ALTER COLUMN ""ReportSource"" TYPE integer 
                USING (CASE 
                    WHEN ""ReportSource"" = '1' OR ""ReportSource"" = 'RIS' THEN 1
                    WHEN ""ReportSource"" = '2' OR ""ReportSource"" = 'LIS' THEN 2
                    ELSE 1
                END)::integer;
            ");

            migrationBuilder.AlterColumn<string>(
                name: "Impression",
                table: "InspectionReports",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "Findings",
                table: "InspectionReports",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "AttachmentUrl",
                table: "InspectionReports",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddColumn<DateTime>(
                name: "CreateTime",
                table: "InspectionReports",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AlterColumn<string>(
                name: "ReportId",
                table: "InspectionOrders",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "Precautions",
                table: "InspectionOrders",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "AppointmentPlace",
                table: "InspectionOrders",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddColumn<int>(
                name: "InspectionStatus",
                table: "InspectionOrders",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Source",
                table: "InspectionOrders",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AlterColumn<int>(
                name: "Id",
                table: "HospitalTimeSlots",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer")
                .OldAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

            migrationBuilder.AddColumn<bool>(
                name: "IsRolledBack",
                table: "ExecutionTasks",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreateTime",
                table: "Departments",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "CreateTime",
                table: "Beds",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.CreateIndex(
                name: "IX_Wards_DepartmentId",
                table: "Wards",
                column: "DepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_InspectionReports_ReviewerId",
                table: "InspectionReports",
                column: "ReviewerId");

            migrationBuilder.AddForeignKey(
                name: "FK_ExecutionTasks_MedicalOrders_MedicalOrderId",
                table: "ExecutionTasks",
                column: "MedicalOrderId",
                principalTable: "MedicalOrders",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_InspectionReports_Doctors_ReviewerId",
                table: "InspectionReports",
                column: "ReviewerId",
                principalTable: "Doctors",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Wards_Departments_DepartmentId",
                table: "Wards",
                column: "DepartmentId",
                principalTable: "Departments",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ExecutionTasks_MedicalOrders_MedicalOrderId",
                table: "ExecutionTasks");

            migrationBuilder.DropForeignKey(
                name: "FK_InspectionReports_Doctors_ReviewerId",
                table: "InspectionReports");

            migrationBuilder.DropForeignKey(
                name: "FK_Wards_Departments_DepartmentId",
                table: "Wards");

            migrationBuilder.DropIndex(
                name: "IX_Wards_DepartmentId",
                table: "Wards");

            migrationBuilder.DropIndex(
                name: "IX_InspectionReports_ReviewerId",
                table: "InspectionReports");

            migrationBuilder.DropColumn(
                name: "CreateTime",
                table: "Wards");

            migrationBuilder.DropColumn(
                name: "CreateTime",
                table: "ShiftTypes");

            migrationBuilder.DropColumn(
                name: "CreateTime",
                table: "Patients");

            migrationBuilder.DropColumn(
                name: "CreateTime",
                table: "InspectionReports");

            migrationBuilder.DropColumn(
                name: "InspectionStatus",
                table: "InspectionOrders");

            migrationBuilder.DropColumn(
                name: "Source",
                table: "InspectionOrders");

            migrationBuilder.DropColumn(
                name: "IsRolledBack",
                table: "ExecutionTasks");

            migrationBuilder.DropColumn(
                name: "CreateTime",
                table: "Departments");

            migrationBuilder.DropColumn(
                name: "CreateTime",
                table: "Beds");

            migrationBuilder.RenameColumn(
                name: "DepartmentId",
                table: "Wards",
                newName: "DeptId");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "Wards",
                newName: "WardId");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "ShiftTypes",
                newName: "ShiftId");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "Patients",
                newName: "PatientId");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "InspectionReports",
                newName: "ReportId");

            migrationBuilder.RenameColumn(
                name: "MedicalOrderId",
                table: "ExecutionTasks",
                newName: "OrderId");

            migrationBuilder.RenameIndex(
                name: "IX_ExecutionTasks_MedicalOrderId",
                table: "ExecutionTasks",
                newName: "IX_ExecutionTasks_OrderId");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "Departments",
                newName: "DeptId");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "Beds",
                newName: "BedId");

            migrationBuilder.AddColumn<string>(
                name: "DepartmentDeptId",
                table: "Wards",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "OrderId",
                table: "MedicalOrders",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AlterColumn<string>(
                name: "ReviewerId",
                table: "InspectionReports",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "ReportStatus",
                table: "InspectionReports",
                type: "text",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<string>(
                name: "ReportSource",
                table: "InspectionReports",
                type: "text",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<string>(
                name: "Impression",
                table: "InspectionReports",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Findings",
                table: "InspectionReports",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "AttachmentUrl",
                table: "InspectionReports",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "ReportId",
                table: "InspectionOrders",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Precautions",
                table: "InspectionOrders",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "AppointmentPlace",
                table: "InspectionOrders",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "Id",
                table: "HospitalTimeSlots",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer")
                .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

            migrationBuilder.AddColumn<int>(
                name: "SlotId",
                table: "HospitalTimeSlots",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Wards_DepartmentDeptId",
                table: "Wards",
                column: "DepartmentDeptId");

            migrationBuilder.AddForeignKey(
                name: "FK_ExecutionTasks_MedicalOrders_OrderId",
                table: "ExecutionTasks",
                column: "OrderId",
                principalTable: "MedicalOrders",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Wards_Departments_DepartmentDeptId",
                table: "Wards",
                column: "DepartmentDeptId",
                principalTable: "Departments",
                principalColumn: "DeptId");
        }
    }
}
