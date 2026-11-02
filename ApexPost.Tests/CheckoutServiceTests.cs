namespace ApexPost.Tests;

using ApexPost.Core.Application.Services;
using ApexPost.Core.Domain;

public sealed class CheckoutServiceTests
{
    private static readonly BranchCode Branch = new("BR-001");
    private static readonly Guid Clerk = Guid.Parse("11111111-1111-1111-1111-111111111111");

    private sealed record Harness(
        CheckoutService Service,
        InMemoryStockRepository Stock,
        InMemoryPaymentRepository Payments,
        SpyUnitOfWork UnitOfWork,
        RecordingNotificationSink Sink);

    private static Harness Build(int stockOnHand, int reorderThreshold = 2)
    {
        var stock = new InMemoryStockRepository();
        stock.Items.Add(new StockItem
        {
            BranchCode = Branch,
            ProductSku = new Sku("SKU-1001"),
            QuantityOnHand = stockOnHand,
            ReorderThreshold = reorderThreshold
        });

        var payments = new InMemoryPaymentRepository();
        var uow = new SpyUnitOfWork();
        var sink = new RecordingNotificationSink();
        var service = new CheckoutService(stock, new InMemoryTransactionRepository(), payments, sink, uow);
        return new Harness(service, stock, payments, uow, sink);
    }

    private static TransactionLine Retail(int quantity) => new()
    {
        Description = "Bubble wrap",
        ProductSku = new Sku("SKU-1001"),
        Quantity = quantity,
        UnitPrice = new Money(8.50m),
        TaxCategory = TaxCategory.Standard
    };

    [Fact]
    public async Task Successful_checkout_commits_exactly_once_and_decrements_stock()
    {
        var h = Build(stockOnHand: 10);

        var sale = await h.Service.CheckoutAsync(Branch, "C1", Clerk, new List<TransactionLine> { Retail(2) }, PaymentMethod.Card);

        Assert.Equal(TransactionStatus.Paid, sale.Status);
        Assert.Equal(1, h.UnitOfWork.SaveChangesCalls);
        Assert.Equal(8, h.Stock.Items[0].QuantityOnHand);
        Assert.Single(h.Payments.Items);
    }

    [Fact]
    public async Task Insufficient_stock_throws_and_never_commits()
    {
        var h = Build(stockOnHand: 1);

        await Assert.ThrowsAsync<DomainException>(() =>
            h.Service.CheckoutAsync(Branch, "C1", Clerk, new List<TransactionLine> { Retail(2) }, PaymentMethod.Card));

        Assert.Equal(0, h.UnitOfWork.SaveChangesCalls);
        Assert.Equal(1, h.Stock.Items[0].QuantityOnHand);
        Assert.Empty(h.Payments.Items);
    }

    [Fact]
    public async Task Two_lines_for_the_same_sku_are_checked_together()
    {
        var h = Build(stockOnHand: 3);

        await Assert.ThrowsAsync<DomainException>(() =>
            h.Service.CheckoutAsync(Branch, "C1", Clerk, new List<TransactionLine> { Retail(2), Retail(2) }, PaymentMethod.Card));

        Assert.Equal(0, h.UnitOfWork.SaveChangesCalls);
        Assert.Equal(3, h.Stock.Items[0].QuantityOnHand);
    }

    [Fact]
    public async Task Empty_basket_is_rejected_before_any_write()
    {
        var h = Build(stockOnHand: 5);

        await Assert.ThrowsAsync<DomainException>(() =>
            h.Service.CheckoutAsync(Branch, "C1", Clerk, new List<TransactionLine>(), PaymentMethod.Card));

        Assert.Equal(0, h.UnitOfWork.SaveChangesCalls);
    }

    [Fact]
    public async Task Stock_falling_to_the_reorder_level_publishes_a_low_stock_event()
    {
        var h = Build(stockOnHand: 3, reorderThreshold: 2);

        await h.Service.CheckoutAsync(Branch, "C1", Clerk, new List<TransactionLine> { Retail(2) }, PaymentMethod.Card);

        Assert.Contains(h.Sink.Events, e => e.EventType == "StockLow");
        Assert.Contains(h.Sink.Events, e => e.EventType == "TransactionCompleted");
    }
}