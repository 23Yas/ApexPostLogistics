namespace ApexPost.Core.Application.Services;

using ApexPost.Core.Application.Interfaces;
using ApexPost.Core.Domain;

public interface INotificationSink
{
    Task PublishAsync(DomainEvent domainEvent, CancellationToken cancellationToken = default);
}

public sealed class TransactionService
{
    private readonly IStockRepository _stockRepository;
    private readonly ITransactionRepository _transactionRepository;
    private readonly INotificationSink _notificationSink;
    private readonly PricingService _pricingService;

    public TransactionService(
        IStockRepository stockRepository,
        ITransactionRepository transactionRepository,
        INotificationSink notificationSink,
        PricingService pricingService)
    {
        _stockRepository = stockRepository;
        _transactionRepository = transactionRepository;
        _notificationSink = notificationSink;
        _pricingService = pricingService;
    }

    public async Task<CounterTransaction> CreateTransactionAsync(
        BranchCode branchCode,
        string counterId,
        Guid clerkId,
        IReadOnlyCollection<TransactionLine> lineItems,
        PaymentMethod paymentMethod,
        CancellationToken cancellationToken = default)
    {
        var transaction = new CounterTransaction
        {
            BranchCode = branchCode,
            CounterId = counterId,
            ClerkId = clerkId,
            PaymentMethod = paymentMethod,
            LineItems = [.. lineItems],
            Status = TransactionStatus.Open
        };

        transaction.SubTotal = new Money(lineItems.Sum(line => line.UnitPrice.Amount * line.Quantity));
        transaction.TaxTotal = new Money(lineItems.Sum(line => PricingService.CalculateTaxFor(new Money(line.UnitPrice.Amount * line.Quantity), line.TaxCategory).Amount));
        transaction.Total = transaction.SubTotal + transaction.TaxTotal;

        await _transactionRepository.AddAsync(transaction, cancellationToken);
        return transaction;
    }

    public async Task ValidateStockAsync(BranchCode branchCode, IReadOnlyCollection<TransactionLine> lines, CancellationToken cancellationToken = default)
    {
        foreach (var line in lines)
        {
            if (line.ProductSku is null)
            {
                continue;
            }

            var stock = await _stockRepository.GetByBranchAndSkuAsync(branchCode, line.ProductSku.Value, cancellationToken);
            if (stock is null || stock.QuantityOnHand < line.Quantity)
            {
                throw new InvalidOperationException($"Insufficient stock for SKU {line.ProductSku.Value}.");
            }
        }
    }

    public async Task CompleteTransactionAsync(CounterTransaction transaction, CancellationToken cancellationToken = default)
    {
        foreach (var line in transaction.LineItems)
        {
            if (line.ProductSku is null)
            {
                continue;
            }

            var stock = await _stockRepository.GetByBranchAndSkuAsync(transaction.BranchCode, line.ProductSku.Value, cancellationToken);
            if (stock is not null)
            {
                stock.QuantityOnHand -= line.Quantity;
                await _stockRepository.UpdateAsync(stock, cancellationToken);
            }

            if (stock is not null && stock.QuantityOnHand <= stock.ReorderThreshold)
            {
                await _notificationSink.PublishAsync(new DomainEvent
                {
                    EventType = "StockLow",
                    BranchCode = transaction.BranchCode,
                    Message = $"SKU {line.ProductSku.Value} is low at {stock.QuantityOnHand}."
                }, cancellationToken);
            }
        }

        transaction.Status = TransactionStatus.Paid;
        transaction.ClosedAtUtc = DateTimeOffset.UtcNow;
        await _transactionRepository.UpdateAsync(transaction, cancellationToken);

        await _notificationSink.PublishAsync(new DomainEvent
        {
            EventType = "TransactionCompleted",
            BranchCode = transaction.BranchCode,
            Message = $"Transaction {transaction.Id} completed."
        }, cancellationToken);
    }

    public async Task VoidAsync(CounterTransaction transaction, Guid managerId, string reason, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new InvalidOperationException("A void requires a reason.");
        }

        transaction.Status = TransactionStatus.Voided;
        transaction.VoidReason = reason;
        transaction.ClosedAtUtc = DateTimeOffset.UtcNow;
        await _transactionRepository.UpdateAsync(transaction, cancellationToken);

        await _notificationSink.PublishAsync(new DomainEvent
        {
            EventType = "TransactionVoided",
            BranchCode = transaction.BranchCode,
            Message = $"Transaction {transaction.Id} voided by {managerId}: {reason}"
        }, cancellationToken);
    }

    public async Task RefundAsync(CounterTransaction transaction, Guid managerId, string reason, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new InvalidOperationException("A refund requires a reason.");
        }

        transaction.Status = TransactionStatus.Refunded;
        transaction.VoidReason = reason;
        transaction.ClosedAtUtc = DateTimeOffset.UtcNow;
        await _transactionRepository.UpdateAsync(transaction, cancellationToken);

        await _notificationSink.PublishAsync(new DomainEvent
        {
            EventType = "TransactionRefunded",
            BranchCode = transaction.BranchCode,
            Message = $"Transaction {transaction.Id} refunded by {managerId}: {reason}"
        }, cancellationToken);
    }
}
