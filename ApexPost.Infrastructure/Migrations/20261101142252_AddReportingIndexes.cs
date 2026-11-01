using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ApexPost.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddReportingIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_TransactionLines_ServiceCode",
                table: "TransactionLines",
                column: "ServiceCode");

            migrationBuilder.CreateIndex(
                name: "IX_MoneyOrders_BranchCode_CreatedAtUtc",
                table: "MoneyOrders",
                columns: new[] { "BranchCode", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_CounterTransactions_BranchCode_CreatedAtUtc",
                table: "CounterTransactions",
                columns: new[] { "BranchCode", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_BranchStock_BranchCode_QuantityOnHand",
                table: "BranchStock",
                columns: new[] { "BranchCode", "QuantityOnHand" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_TransactionLines_ServiceCode",
                table: "TransactionLines");

            migrationBuilder.DropIndex(
                name: "IX_MoneyOrders_BranchCode_CreatedAtUtc",
                table: "MoneyOrders");

            migrationBuilder.DropIndex(
                name: "IX_CounterTransactions_BranchCode_CreatedAtUtc",
                table: "CounterTransactions");

            migrationBuilder.DropIndex(
                name: "IX_BranchStock_BranchCode_QuantityOnHand",
                table: "BranchStock");
        }
    }
}
