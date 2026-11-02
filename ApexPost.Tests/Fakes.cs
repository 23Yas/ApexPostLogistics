namespace ApexPost.Tests;

using ApexPost.Core.Application.Interfaces;
using ApexPost.Core.Application.Services;
using ApexPost.Core.Domain;

internal abstract class InMemoryRepository<T> : IRepository<T> where T : class
{
    public List<T> Items { get; } = new();

    public Task<IReadOnlyCollection<T>> GetAllAsync(CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyCollection<T>>(Items.ToList());

    public Task<T?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => Task.FromResult<T?>(null);

    public Task AddAsync(T entity, CancellationToken cancellationToken = default)
    {
        Items.Add(entity);
        return Task.CompletedTask;
    }

    public Task UpdateAsync(T entity, CancellationToken cancellationToken = default)
        => Task.CompletedTask;

    public Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
        => Task.CompletedTask;
}

internal sealed class InMemoryStockRepository : InMemoryRepository<StockItem>, IStockRepository
{
    public Task<StockItem?> GetByBranchAndSkuAsync(BranchCode branchCode, Sku sku, CancellationToken cancellationToken = default)
        => Task.FromResult(Items.FirstOrDefault(item => item.BranchCode == branchCode && item.ProductSku == sku));

    public Task<IReadOnlyCollection<StockItem>> GetLowStockAsync(BranchCode branchCode, int threshold, CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyCollection<StockItem>>(Items.Where(item => item.BranchCode == branchCode && item.QuantityOnHand <= threshold).ToList());
}

internal sealed class InMemoryTransactionRepository : InMemoryRepository<CounterTransaction>, ITransactionRepository
{
    public Task<IReadOnlyCollection<CounterTransaction>> GetByBranchAsync(BranchCode branchCode, CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyCollection<CounterTransaction>>(Items.Where(tx => tx.BranchCode == branchCode).ToList());

    public Task<IReadOnlyCollection<CounterTransaction>> GetByDateAsync(DateOnly date, BranchCode? branchCode = null, CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyCollection<CounterTransaction>>(Items.Where(tx => DateOnly.FromDateTime(tx.CreatedAtUtc.UtcDateTime) == date).ToList());
}

internal sealed class InMemoryPaymentRepository : InMemoryRepository<Payment>, IPaymentRepository
{
}

/// <summary>Counts commits. A use case must commit exactly once on success and never on failure.</summary>
internal sealed class SpyUnitOfWork : IUnitOfWork
{
    public int SaveChangesCalls { get; private set; }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        SaveChangesCalls++;
        return Task.FromResult(1);
    }

    public Task ExecuteInTransactionAsync(Func<CancellationToken, Task> work, CancellationToken cancellationToken = default)
        => work(cancellationToken);
}

internal sealed class RecordingNotificationSink : INotificationSink
{
    public List<DomainEvent> Events { get; } = new();

    public Task PublishAsync(DomainEvent domainEvent, CancellationToken cancellationToken = default)
    {
        Events.Add(domainEvent);
        return Task.CompletedTask;
    }
}