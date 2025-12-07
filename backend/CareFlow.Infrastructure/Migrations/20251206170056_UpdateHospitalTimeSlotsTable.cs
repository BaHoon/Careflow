using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace CareFlow.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdateHospitalTimeSlotsTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ExecutionTasks_MedicalOrders_OrderId",
                table: "ExecutionTasks");

            migrationBuilder.DropForeignKey(
                name: "FK_InspectionOrders_MedicalOrders_OrderId",
                table: "InspectionOrders");

            migrationBuilder.DropForeignKey(
                name: "FK_MedicationOrders_MedicalOrders_OrderId",
                table: "MedicationOrders");

            migrationBuilder.DropForeignKey(
                name: "FK_OperationOrders_MedicalOrders_OrderId",
                table: "OperationOrders");

            migrationBuilder.DropForeignKey(
                name: "FK_SurgicalOrders_MedicalOrders_OrderId",
                table: "SurgicalOrders");

            migrationBuilder.DropPrimaryKey(
                name: "PK_MedicalOrders",
                table: "MedicalOrders");

            migrationBuilder.DropPrimaryKey(
                name: "PK_HospitalTimeSlots",
                table: "HospitalTimeSlots");

            migrationBuilder.RenameColumn(
                name: "OrderId",
                table: "SurgicalOrders",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "OrderId",
                table: "OperationOrders",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "OrderId",
                table: "MedicationOrders",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "OrderId",
                table: "InspectionOrders",
                newName: "Id");

            migrationBuilder.AlterColumn<long>(
                name: "OrderId",
                table: "MedicalOrders",
                type: "bigint",
                nullable: false,
                oldClrType: typeof(long),
                oldType: "bigint")
                .OldAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

            migrationBuilder.AddColumn<long>(
                name: "Id",
                table: "MedicalOrders",
                type: "bigint",
                nullable: false,
                defaultValue: 0L)
                .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

            migrationBuilder.AddColumn<int>(
                name: "Id",
                table: "HospitalTimeSlots",
                type: "integer",
                nullable: false,
                defaultValue: 0)
                .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreateTime",
                table: "HospitalTimeSlots",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddPrimaryKey(
                name: "PK_MedicalOrders",
                table: "MedicalOrders",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_HospitalTimeSlots",
                table: "HospitalTimeSlots",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ExecutionTasks_MedicalOrders_OrderId",
                table: "ExecutionTasks",
                column: "OrderId",
                principalTable: "MedicalOrders",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_InspectionOrders_MedicalOrders_Id",
                table: "InspectionOrders",
                column: "Id",
                principalTable: "MedicalOrders",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_MedicationOrders_MedicalOrders_Id",
                table: "MedicationOrders",
                column: "Id",
                principalTable: "MedicalOrders",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_OperationOrders_MedicalOrders_Id",
                table: "OperationOrders",
                column: "Id",
                principalTable: "MedicalOrders",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_SurgicalOrders_MedicalOrders_Id",
                table: "SurgicalOrders",
                column: "Id",
                principalTable: "MedicalOrders",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ExecutionTasks_MedicalOrders_OrderId",
                table: "ExecutionTasks");

            migrationBuilder.DropForeignKey(
                name: "FK_InspectionOrders_MedicalOrders_Id",
                table: "InspectionOrders");

            migrationBuilder.DropForeignKey(
                name: "FK_MedicationOrders_MedicalOrders_Id",
                table: "MedicationOrders");

            migrationBuilder.DropForeignKey(
                name: "FK_OperationOrders_MedicalOrders_Id",
                table: "OperationOrders");

            migrationBuilder.DropForeignKey(
                name: "FK_SurgicalOrders_MedicalOrders_Id",
                table: "SurgicalOrders");

            migrationBuilder.DropPrimaryKey(
                name: "PK_MedicalOrders",
                table: "MedicalOrders");

            migrationBuilder.DropPrimaryKey(
                name: "PK_HospitalTimeSlots",
                table: "HospitalTimeSlots");

            migrationBuilder.DropColumn(
                name: "Id",
                table: "MedicalOrders");

            migrationBuilder.DropColumn(
                name: "Id",
                table: "HospitalTimeSlots");

            migrationBuilder.DropColumn(
                name: "CreateTime",
                table: "HospitalTimeSlots");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "SurgicalOrders",
                newName: "OrderId");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "OperationOrders",
                newName: "OrderId");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "MedicationOrders",
                newName: "OrderId");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "InspectionOrders",
                newName: "OrderId");

            migrationBuilder.AlterColumn<long>(
                name: "OrderId",
                table: "MedicalOrders",
                type: "bigint",
                nullable: false,
                oldClrType: typeof(long),
                oldType: "bigint")
                .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

            migrationBuilder.AddPrimaryKey(
                name: "PK_MedicalOrders",
                table: "MedicalOrders",
                column: "OrderId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_HospitalTimeSlots",
                table: "HospitalTimeSlots",
                column: "SlotId");

            migrationBuilder.AddForeignKey(
                name: "FK_ExecutionTasks_MedicalOrders_OrderId",
                table: "ExecutionTasks",
                column: "OrderId",
                principalTable: "MedicalOrders",
                principalColumn: "OrderId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_InspectionOrders_MedicalOrders_OrderId",
                table: "InspectionOrders",
                column: "OrderId",
                principalTable: "MedicalOrders",
                principalColumn: "OrderId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_MedicationOrders_MedicalOrders_OrderId",
                table: "MedicationOrders",
                column: "OrderId",
                principalTable: "MedicalOrders",
                principalColumn: "OrderId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_OperationOrders_MedicalOrders_OrderId",
                table: "OperationOrders",
                column: "OrderId",
                principalTable: "MedicalOrders",
                principalColumn: "OrderId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_SurgicalOrders_MedicalOrders_OrderId",
                table: "SurgicalOrders",
                column: "OrderId",
                principalTable: "MedicalOrders",
                principalColumn: "OrderId",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
