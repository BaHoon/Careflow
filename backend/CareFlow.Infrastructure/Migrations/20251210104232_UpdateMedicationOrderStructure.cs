using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace CareFlow.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdateMedicationOrderStructure : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MedicationOrders_Drugs_DrugId",
                table: "MedicationOrders");

            migrationBuilder.DropColumn(
                name: "Dosage",
                table: "MedicationOrders");

            // 使用 CASE 语句将 UsageRoute 从中文字符串转换为枚举整数值
            migrationBuilder.Sql(@"
                ALTER TABLE ""MedicationOrders"" 
                ALTER COLUMN ""UsageRoute"" 
                TYPE integer 
                USING CASE ""UsageRoute""
                    WHEN '口服' THEN 1          -- PO
                    WHEN '外用涂抹' THEN 2      -- Topical
                    WHEN '外用/涂抹' THEN 2     -- Topical (变体)
                    WHEN '肌肉注射' THEN 10     -- IM
                    WHEN '皮下注射' THEN 11     -- SC
                    WHEN '静脉推注' THEN 12     -- IVP
                    WHEN '静脉滴注' THEN 20     -- IVGTT
                    WHEN '鼻导管吸氧' THEN 21   -- Inhalation
                    WHEN '吸氧' THEN 21         -- Inhalation (简化形式)
                    WHEN '皮试' THEN 30         -- ST
                    ELSE 1                      -- 默认为 PO (口服)
                END;
            ");

            migrationBuilder.AlterColumn<string>(
                name: "DrugId",
                table: "MedicationOrders",
                type: "character varying(50)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(50)");

            migrationBuilder.CreateTable(
                name: "MedicationOrderItems",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    MedicationOrderId = table.Column<long>(type: "bigint", nullable: false),
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
                        name: "FK_MedicationOrderItems_MedicationOrders_MedicationOrderId",
                        column: x => x.MedicationOrderId,
                        principalTable: "MedicationOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MedicationOrderItems_DrugId",
                table: "MedicationOrderItems",
                column: "DrugId");

            migrationBuilder.CreateIndex(
                name: "IX_MedicationOrderItems_MedicationOrderId",
                table: "MedicationOrderItems",
                column: "MedicationOrderId");

            migrationBuilder.AddForeignKey(
                name: "FK_MedicationOrders_Drugs_DrugId",
                table: "MedicationOrders",
                column: "DrugId",
                principalTable: "Drugs",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MedicationOrders_Drugs_DrugId",
                table: "MedicationOrders");

            migrationBuilder.DropTable(
                name: "MedicationOrderItems");

            // 将 UsageRoute 从整数转换回中文字符串
            migrationBuilder.Sql(@"
                ALTER TABLE ""MedicationOrders"" 
                ALTER COLUMN ""UsageRoute"" 
                TYPE text 
                USING CASE ""UsageRoute""
                    WHEN 1 THEN '口服'          -- PO
                    WHEN 2 THEN '外用涂抹'      -- Topical
                    WHEN 10 THEN '肌肉注射'     -- IM
                    WHEN 11 THEN '皮下注射'     -- SC
                    WHEN 12 THEN '静脉推注'     -- IVP
                    WHEN 20 THEN '静脉滴注'     -- IVGTT
                    WHEN 21 THEN '鼻导管吸氧'   -- Inhalation
                    WHEN 30 THEN '皮试'         -- ST
                    ELSE '口服'                 -- 默认
                END;
            ");

            migrationBuilder.AlterColumn<string>(
                name: "DrugId",
                table: "MedicationOrders",
                type: "character varying(50)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Dosage",
                table: "MedicationOrders",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddForeignKey(
                name: "FK_MedicationOrders_Drugs_DrugId",
                table: "MedicationOrders",
                column: "DrugId",
                principalTable: "Drugs",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
