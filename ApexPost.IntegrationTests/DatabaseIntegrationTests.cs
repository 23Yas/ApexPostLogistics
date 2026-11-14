namespace ApexPost.Tests;

using ApexPost.Core.Application.Services;
using ApexPost.Core.Domain;
using ApexPost.Infrastructure;
using Microsoft.EntityFrameworkCore;

public sealed class DatabaseIntegrationTests : IClassFixture<CustomWebApplicationFactory>
{
    private static readonly BranchCode Branch = new("BR-001");
    private static readonly Guid Clerk = Guid.Parse("11111111-1111-1111-1111-111111111111");

    private readonly CustomWebApplicationFactory _factory;

    public DatabaseIntegrationTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _factory.EnsureDatabaseReady();
    }

    private static CheckoutService BuildCheckout(ApexPostDbContext context) => new(
        new EfStockRepository(context),
        new EfTransactionRepository(context),
        new EfPaymentRepository(context),
        new RecordingNotificationSink(),
        new UnitOfWork(context));

    [Fact]
    public async Task FailedSale_RollsBack_StockAndTransactionTogether()
    {
        int before;
        int transactionsBefore;
        await using (var reader = _factory.CreateDbContext())
        {
            before = (await reader.StockItems.SingleAsync(s => s.BranchCode == Branch && s.ProductSku == new Sku("SKU-1001"))).QuantityOnHand;
            transactionsBefore = await reader.CounterTransactions.CountAsync();
        }

        // A zero-priced line gives a zero payment, which violates CK_Payments_Amount_Positive at SaveChanges.
        var freeLine = new TransactionLine
        {
            Description = "Free item",
            ProductSku = new Sku("SKU-1001"),
            Quantity = 1,
            UnitPrice = new Money(0m),
            TaxCategory = TaxCategory.Standard
        };

        await using (var writer = _factory.CreateDbContext())
        {
            var ex = await Assert.ThrowsAsync<DbUpdateException>(() =>
                BuildCheckout(writer).CheckoutAsync(Branch, "C1", Clerk, new List<TransactionLine> { freeLine }, PaymentMethod.Card));

            Assert.Contains("CK_Payments_Amount_Positive", ex.InnerException?.Message ?? string.Empty);
        }

        await using var check = _factory.CreateDbContext();
        var after = (await check.StockItems.SingleAsync(s => s.BranchCode == Branch && s.ProductSku == new Sku("SKU-1001"))).QuantityOnHand;
        Assert.Equal(before, after);
        Assert.Equal(transactionsBefore, await check.CounterTransactions.CountAsync());
    }

    [Fact]
    public async Task Two_clerks_selling_the_last_unit_second_save_gets_a_concurrency_conflict()
    {
        var branch = new BranchCode("BR-005");
        var sku = new Sku("SKU-1011");

        await using (var setup = _factory.CreateDbContext())
        {
            var row = await setup.StockItems.SingleAsync(s => s.BranchCode == branch && s.ProductSku == sku);
            row.QuantityOnHand = 1;
            await setup.SaveChangesAsync();
        }

        await using var clerkA = _factory.CreateDbContext();
        await using var clerkB = _factory.CreateDbContext();

        var stockA = await new EfStockRepository(clerkA).GetByBranchAndSkuAsync(branch, sku);
        var stockB = await new EfStockRepository(clerkB).GetByBranchAndSkuAsync(branch, sku);
        stockA!.QuantityOnHand -= 1;
        stockB!.QuantityOnHand -= 1;

        await clerkA.SaveChangesAsync();
        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => clerkB.SaveChangesAsync());

        await using var check = _factory.CreateDbContext();
        var final = await check.StockItems.SingleAsync(s => s.BranchCode == branch && s.ProductSku == sku);
        Assert.Equal(0, final.QuantityOnHand);
    }

    [Fact]
    public async Task LowStockProcedure_returns_every_stock_row_below_a_high_threshold()
    {
        await using var context = _factory.CreateDbContext();
        var rows = await new EfReportQueryService(context).GetLowStockAsync(null, 100000);
        var total = await context.StockItems.CountAsync();

        Assert.Equal(total, rows.Count);
    }

    [Fact]
    public async Task DailyBranchSalesProcedure_returns_zero_totals_for_a_day_without_sales()
    {
        await using var context = _factory.CreateDbContext();
        var row = await new EfReportQueryService(context).GetDailyBranchSalesAsync(Branch, new DateOnly(2099, 1, 1));

        Assert.Equal(0m, row.RetailTotal);
        Assert.Equal(0m, row.ServiceTotal);
    }
}