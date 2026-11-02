namespace ApexPost.Core.Application.Interfaces;

using ApexPost.Core.Domain;

public interface IRepository<T> where T : class
{
    Task<IReadOnlyCollection<T>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<T?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task AddAsync(T entity, CancellationToken cancellationToken = default);
    Task UpdateAsync(T entity, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}

public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    Task ExecuteInTransactionAsync(Func<CancellationToken, Task> work, CancellationToken cancellationToken = default);
}

public interface IBranchRepository : IRepository<Branch>
{
    Task<Branch?> GetByCodeAsync(BranchCode code, CancellationToken cancellationToken = default);
}

public interface IStaffRepository : IRepository<Staff>
{
    Task<Staff?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);
}

public interface IProductRepository : IRepository<Product>
{
    Task<Product?> GetBySkuAsync(Sku sku, CancellationToken cancellationToken = default);
    ValueTask<IReadOnlyCollection<Product>> GetCachedProductsAsync(CancellationToken cancellationToken = default);
}

public interface IStockRepository : IRepository<StockItem>
{
    Task<StockItem?> GetByBranchAndSkuAsync(BranchCode branchCode, Sku sku, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<StockItem>> GetLowStockAsync(BranchCode branchCode, int threshold, CancellationToken cancellationToken = default);
}

public interface ICounterTransactionRepository : IRepository<CounterTransaction>
{
    Task<IReadOnlyCollection<CounterTransaction>> GetByBranchAsync(BranchCode branchCode, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<CounterTransaction>> GetByDateAsync(DateOnly date, BranchCode? branchCode = null, CancellationToken cancellationToken = default);
}

public interface ITransactionRepository : ICounterTransactionRepository
{
}

public interface IMoneyOrderRepository : IRepository<MoneyOrder>
{
    Task<IReadOnlyCollection<MoneyOrder>> GetByBranchAsync(BranchCode branchCode, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<MoneyOrder>> GetByBranchAndDateAsync(BranchCode branchCode, DateTime date, CancellationToken cancellationToken = default);
}

public interface IAuditStore
{
    Task AppendAsync(AuditEntry entry, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<AuditEntry>> ReadAsync(CancellationToken cancellationToken = default);
}

public sealed record AuditEntry(
    Guid Id,
    Guid StaffId,
    string Role,
    string BranchCode,
    string Controller,
    string Action,
    string CorrelationId,
    string ResultStatus,
    string RouteValues,
    double DurationMs,
    DateTimeOffset RecordedAtUtc,
    string Details,
    string[] RedactedFields,
    string? TransactionId = null,
    string? Reason = null);
