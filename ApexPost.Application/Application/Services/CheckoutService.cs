namespace ApexPost.Core.Application.Services;

using ApexPost.Core.Application.Interfaces;
using ApexPost.Core.Domain;

/// <summary>
/// One use case, one commit: a counter sale decrements stock and stages the transaction, its lines
/// and the payment, then calls SaveChangesAsync exactly once. If another clerk sold the last unit
/// in the meantime, the stock rowversion check makes SaveChangesAsync throw
/// DbUpdateConcurrencyException (mapped to HTTP 409 by the web layer) and nothing is persisted.
/// </summary>
public sealed class CheckoutService
{
    private readonly IStockRepository _stockRepository;
    private readonly ITransactionRepository _transactionRepository;
    private readonly IPaymentRepository _paymentRepository;
    private readonly INotificationSink _notificationSink;
    private readonly IUnitOfWork _unitOfWork;

    public CheckoutService(
        IStockRepository stockRepository,
        ITransactionRepository transactionRepository,
        IPaymentRepository paymentRepository,
        INotificationSink notificationSink,
        IUnitOfWork unitOfWork)
    {
        _stockRepository = stockRepository;
        _transactionRepository = transactionRepository;
        _paymentRepository = paymentRepository;
        _notificationSink = notificationSink;
        _unitOfWork = unitOfWork;
    }

    public async Task<CounterTransaction> CheckoutAsync(
        BranchCode branchCode,
        string counterId,
        Guid clerkId,
        IReadOnlyCollection<TransactionLine> lines,
        PaymentMethod paymentMethod,
        CancellationToken cancellationToken = default)
    {
        if (lines.Count == 0)
        {
            throw new DomainException("A transaction requires at least one line item.");
        }

        if (lines.Any(line => line.Quantity <= 0))
        {
            throw new DomainException("Every line must have a quantity of at least 1.");
        }

        // Pass 1: validate everything before changing anything (aggregate per SKU so two lines
        // for the same SKU cannot oversell).
        var needed = lines
            .Where(line => line.ProductSku is not null)
            .GroupBy(line => line.ProductSku!.Value)
            .Select(group => (Sku: group.Key, Quantity: group.Sum(line => line.Quantity)))
            .ToList();

        var stockRows = new List<(StockItem Stock, int Quantity)>();
        foreach (var (sku, quantity) in needed)
        {
            var stock = await _stockRepository.GetByBranchAndSkuAsync(branchCode, sku, cancellationToken);
            if (stock is null || stock.QuantityOnHand < quantity)
            {
                throw new DomainException($"Insufficient stock for SKU {sku}.");
            }

            stockRows.Add((stock, quantity));
        }

        // Pass 2: mutate.
        var events = new List<DomainEvent>();
        foreach (var (stock, quantity) in stockRows)
        {
            stock.QuantityOnHand -= quantity;
            await _stockRepository.UpdateAsync(stock, cancellationToken);

            if (stock.QuantityOnHand <= stock.ReorderThreshold)
            {
                events.Add(new DomainEvent
                {
                    EventType = "StockLow",
                    BranchCode = branchCode,
                    Message = $"SKU {stock.ProductSku} is low at {stock.QuantityOnHand}."
                });
            }
        }

        var subTotal = new Money(lines.Sum(line => line.UnitPrice.Amount * line.Quantity));
        var taxTotal = new Money(lines.Sum(line =>
            PricingService.CalculateTaxFor(new Money(line.UnitPrice.Amount * line.Quantity), line.TaxCategory).Amount));

        var transaction = new CounterTransaction
        {
            BranchCode = branchCode,
            CounterId = counterId,
            ClerkId = clerkId,
            PaymentMethod = paymentMethod,
            LineItems = [.. lines],
            Status = TransactionStatus.Paid,
            SubTotal = subTotal,
            TaxTotal = taxTotal,
            Total = subTotal + taxTotal,
            ClosedAtUtc = DateTimeOffset.UtcNow
        };

        await _transactionRepository.AddAsync(transaction, cancellationToken);
        await _paymentRepository.AddAsync(new Payment
        {
            CounterTransactionId = transaction.Id,
            Method = paymentMethod,
            Amount = transaction.Total.Amount
        }, cancellationToken);

        // The only commit of this use case.
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        foreach (var domainEvent in events)
        {
            await _notificationSink.PublishAsync(domainEvent, cancellationToken);
        }

        await _notificationSink.PublishAsync(new DomainEvent
        {
            EventType = "TransactionCompleted",
            BranchCode = branchCode,
            Message = $"Transaction {transaction.Id} completed."
        }, cancellationToken);

        return transaction;
    }
}
