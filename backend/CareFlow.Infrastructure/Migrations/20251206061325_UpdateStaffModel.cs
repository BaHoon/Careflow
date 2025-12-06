using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CareFlow.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdateStaffModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Doctors_Staffs_StaffId",
                table: "Doctors");

            migrationBuilder.DropForeignKey(
                name: "FK_Nurses_Staffs_StaffId",
                table: "Nurses");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Staffs",
                table: "Staffs");

            migrationBuilder.RenameColumn(
                name: "StaffId",
                table: "Staffs",
                newName: "EmployeeNumber");

            migrationBuilder.RenameColumn(
                name: "StaffId",
                table: "Nurses",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "StaffId",
                table: "Doctors",
                newName: "Id");

            migrationBuilder.AddColumn<string>(
                name: "Id",
                table: "Staffs",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "CreateTime",
                table: "Staffs",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddPrimaryKey(
                name: "PK_Staffs",
                table: "Staffs",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Doctors_Staffs_Id",
                table: "Doctors",
                column: "Id",
                principalTable: "Staffs",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Nurses_Staffs_Id",
                table: "Nurses",
                column: "Id",
                principalTable: "Staffs",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Doctors_Staffs_Id",
                table: "Doctors");

            migrationBuilder.DropForeignKey(
                name: "FK_Nurses_Staffs_Id",
                table: "Nurses");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Staffs",
                table: "Staffs");

            migrationBuilder.DropColumn(
                name: "Id",
                table: "Staffs");

            migrationBuilder.DropColumn(
                name: "CreateTime",
                table: "Staffs");

            migrationBuilder.RenameColumn(
                name: "EmployeeNumber",
                table: "Staffs",
                newName: "StaffId");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "Nurses",
                newName: "StaffId");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "Doctors",
                newName: "StaffId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Staffs",
                table: "Staffs",
                column: "StaffId");

            migrationBuilder.AddForeignKey(
                name: "FK_Doctors_Staffs_StaffId",
                table: "Doctors",
                column: "StaffId",
                principalTable: "Staffs",
                principalColumn: "StaffId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Nurses_Staffs_StaffId",
                table: "Nurses",
                column: "StaffId",
                principalTable: "Staffs",
                principalColumn: "StaffId",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
