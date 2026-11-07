using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ApexPost.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Week4CourierApi : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_Shipments_Status_Valid",
                table: "Shipments");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Shipments_Weight_Range",
                table: "Shipments");

            migrationBuilder.AlterColumn<int>(
                name: "ZoneId",
                table: "Shipments",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<Guid>(
                name: "CounterTransactionId",
                table: "Shipments",
                type: "uniqueidentifier",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");

            migrationBuilder.AlterColumn<string>(
                name: "BranchCode",
                table: "Shipments",
                type: "varchar(32)",
                unicode: false,
                maxLength: 32,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(32)",
                oldUnicode: false,
                oldMaxLength: 32);

            migrationBuilder.AddColumn<string>(
                name: "RecipientAddress",
                table: "Shipments",
                type: "nvarchar(400)",
                maxLength: 400,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "RecipientCity",
                table: "Shipments",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "RecipientName",
                table: "Shipments",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "RecipientPhone",
                table: "Shipments",
                type: "varchar(32)",
                unicode: false,
                maxLength: 32,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "ShipmentScans",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TrackingNumber = table.Column<string>(type: "varchar(40)", unicode: false, maxLength: 40, nullable: false),
                    ScanType = table.Column<string>(type: "varchar(32)", unicode: false, maxLength: 32, nullable: false),
                    CourierId = table.Column<string>(type: "varchar(64)", unicode: false, maxLength: 64, nullable: false),
                    Location = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ScannedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    CreatedBy = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true, defaultValue: "system"),
                    ModifiedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShipmentScans", x => x.Id);
                });

            migrationBuilder.AddCheckConstraint(
                name: "CK_Shipments_Status_Valid",
                table: "Shipments",
                sql: "Status IN ('Created','PickedUp','InTransit','OutForDelivery','Delivered','Failed','Returned')");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Shipments_Weight_Range",
                table: "Shipments",
                sql: "WeightKg > 0 AND WeightKg <= 70");

            migrationBuilder.CreateIndex(
                name: "IX_ShipmentScans_TrackingNumber",
                table: "ShipmentScans",
                column: "TrackingNumber");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ShipmentScans");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Shipments_Status_Valid",
                table: "Shipments");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Shipments_Weight_Range",
                table: "Shipments");

            migrationBuilder.DropColumn(
                name: "RecipientAddress",
                table: "Shipments");

            migrationBuilder.DropColumn(
                name: "RecipientCity",
                table: "Shipments");

            migrationBuilder.DropColumn(
                name: "RecipientName",
                table: "Shipments");

            migrationBuilder.DropColumn(
                name: "RecipientPhone",
                table: "Shipments");

            migrationBuilder.AlterColumn<int>(
                name: "ZoneId",
                table: "Shipments",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "CounterTransactionId",
                table: "Shipments",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "BranchCode",
                table: "Shipments",
                type: "varchar(32)",
                unicode: false,
                maxLength: 32,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "varchar(32)",
                oldUnicode: false,
                oldMaxLength: 32,
                oldNullable: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_Shipments_Status_Valid",
                table: "Shipments",
                sql: "Status IN ('Created','InTransit','Delivered','Returned')");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Shipments_Weight_Range",
                table: "Shipments",
                sql: "WeightKg > 0 AND WeightKg <= 30");
        }
    }
}
