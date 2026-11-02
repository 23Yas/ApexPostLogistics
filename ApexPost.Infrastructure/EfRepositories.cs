namespace ApexPost.Infrastructure;

using ApexPost.Core.Application.Interfaces;
using ApexPost.Core.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public sealed class UnitOfWork : IUnitOfWork
{
    private readonly ApexPostDbContext _dbContext;

    public UnitOfWork(ApexPostDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        => _dbContext.SaveChangesAsync(cancellationToken);

    public async Task ExecuteInTransactionAsync(Func<CancellationToken, Task> work, CancellationToken cancellationToken = default)
    {
        // Already inside a caller-owned transaction: just participate.
        if (_dbContext.Database.CurrentTransaction is not null)
        {
            await work(cancellationToken);
            return;
        }

        // EnableRetryOnFailure() is on, so a user-initiated transaction MUST run inside the execution strategy.
        // The whole block is re-run on a transient failure; 'work' must therefore be safe to repeat.
        var strategy = _dbContext.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
            await work(cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);   // disposing without commit rolls back
        });
    }
}

public abstract class EfRepository<T> : IRepository<T> where T : class
{
    protected readonly ApexPostDbContext DbContext;

    protected EfRepository(ApexPostDbContext dbContext)
    {
        DbContext = dbContext;
    }

    public virtual async Task<IReadOnlyCollection<T>> GetAllAsync(CancellationToken cancellationToken = default)
        => await DbContext.Set<T>().AsNoTracking().ToListAsync(cancellationToken);

    public virtual async Task<T?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => await DbContext.Set<T>().FindAsync([id], cancellationToken).AsTask();

    public virtual Task AddAsync(T entity, CancellationToken cancellationToken = default)
    {
        DbContext.Set<T>().Add(entity);
        return Task.CompletedTask;
    }

    public virtual Task UpdateAsync(T entity, CancellationToken cancellationToken = default)
    {
        var key = DbContext.Model.FindEntityType(typeof(T))?.FindPrimaryKey();
        if (key is null)
        {
            DbContext.Set<T>().Update(entity);
            return Task.CompletedTask;
        }

        var keyValues = key.Properties
            .Select(property => property.PropertyInfo?.GetValue(entity))
            .ToArray();

        var tracked = DbContext.ChangeTracker.Entries<T>()
            .FirstOrDefault(entry =>
            {
                var currentValues = key.Properties
                    .Select(property => property.PropertyInfo?.GetValue(entry.Entity))
                    .ToArray();

                return currentValues.SequenceEqual(keyValues);
            });

        if (tracked is not null)
        {
            tracked.CurrentValues.SetValues(entity);
            return Task.CompletedTask;
        }

        var local = DbContext.Set<T>().Local.FirstOrDefault(item =>
        {
            var currentValues = key.Properties
                .Select(property => property.PropertyInfo?.GetValue(item))
                .ToArray();

            return currentValues.SequenceEqual(keyValues);
        });

        if (local is not null)
        {
            DbContext.Entry(local).CurrentValues.SetValues(entity);
            return Task.CompletedTask;
        }

        DbContext.Set<T>().Update(entity);
        return Task.CompletedTask;
    }

    public virtual async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await DbContext.Set<T>().FindAsync([id], cancellationToken).AsTask();
        if (entity is not null)
        {
            DbContext.Set<T>().Remove(entity);
        }
    }
}

public sealed class EfBranchRepository : EfRepository<Branch>, IBranchRepository
{
    public EfBranchRepository(ApexPostDbContext dbContext)
        : base(dbContext)
    {
    }

    public async Task<Branch?> GetByCodeAsync(BranchCode code, CancellationToken cancellationToken = default)
        => await DbContext.Branches.AsNoTracking().FirstOrDefaultAsync(branch => branch.Code == code, cancellationToken);
}

public sealed class EfStaffRepository : EfRepository<Staff>, IStaffRepository
{
    public EfStaffRepository(ApexPostDbContext dbContext)
        : base(dbContext)
    {
    }

    public async Task<Staff?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
        => await DbContext.Staff.AsNoTracking().FirstOrDefaultAsync(staff => staff.Email == email, cancellationToken);
}

public sealed class EfProductRepository : EfRepository<Product>, IProductRepository
{
    private IReadOnlyCollection<Product>? _cachedProducts;

    public EfProductRepository(ApexPostDbContext dbContext)
        : base(dbContext)
    {
    }

    public async Task<Product?> GetBySkuAsync(Sku sku, CancellationToken cancellationToken = default)
        => await DbContext.Products.AsNoTracking().FirstOrDefaultAsync(product => product.Sku == sku, cancellationToken);

    public ValueTask<IReadOnlyCollection<Product>> GetCachedProductsAsync(CancellationToken cancellationToken = default)
    {
        if (_cachedProducts is not null)
        {
            return ValueTask.FromResult(_cachedProducts);
        }

        return new ValueTask<IReadOnlyCollection<Product>>(LoadProductsAsync(cancellationToken));
    }

    private async Task<IReadOnlyCollection<Product>> LoadProductsAsync(CancellationToken cancellationToken)
    {
        _cachedProducts = await DbContext.Products.AsNoTracking().ToListAsync(cancellationToken);
        return _cachedProducts;
    }
}

public sealed class EfStockRepository : EfRepository<StockItem>, IStockRepository
{
    public EfStockRepository(ApexPostDbContext dbContext)
        : base(dbContext)
    {
    }

    // Tracked on purpose: callers mutate QuantityOnHand and the loaded RowVersion is the concurrency token.
    // (With AsNoTracking the token is never loaded and the lost-update check does nothing.)
    public async Task<StockItem?> GetByBranchAndSkuAsync(BranchCode branchCode, Sku sku, CancellationToken cancellationToken = default)
        => await DbContext.StockItems.FirstOrDefaultAsync(item => item.BranchCode == branchCode && item.ProductSku == sku, cancellationToken);

    public async Task<IReadOnlyCollection<StockItem>> GetLowStockAsync(BranchCode branchCode, int threshold, CancellationToken cancellationToken = default)
        => await DbContext.StockItems.AsNoTracking().Where(item => item.BranchCode == branchCode && item.QuantityOnHand <= threshold).ToListAsync(cancellationToken);
}

public class EfCounterTransactionRepository : EfRepository<CounterTransaction>, ICounterTransactionRepository
{
    public EfCounterTransactionRepository(ApexPostDbContext dbContext)
        : base(dbContext)
    {
    }

    public async Task<IReadOnlyCollection<CounterTransaction>> GetByBranchAsync(BranchCode branchCode, CancellationToken cancellationToken = default)
        => await DbContext.CounterTransactions.AsNoTracking().Where(transaction => transaction.BranchCode == branchCode).Include(transaction => transaction.LineItems).ToListAsync(cancellationToken);

    public async Task<IReadOnlyCollection<CounterTransaction>> GetByDateAsync(DateOnly date, BranchCode? branchCode = null, CancellationToken cancellationToken = default)
    {
        var start = date.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var end = date.ToDateTime(TimeOnly.MaxValue, DateTimeKind.Utc);
        var query = DbContext.CounterTransactions.AsNoTracking().Where(transaction => transaction.CreatedAtUtc >= start && transaction.CreatedAtUtc <= end);

        if (branchCode is not null)
        {
            query = query.Where(transaction => transaction.BranchCode == branchCode.Value);
        }

        return await query.Include(transaction => transaction.LineItems).ToListAsync(cancellationToken);
    }
}

public sealed class EfTransactionRepository : EfCounterTransactionRepository, ITransactionRepository
{
    public EfTransactionRepository(ApexPostDbContext dbContext)
        : base(dbContext)
    {
    }
}

public sealed class EfMoneyOrderRepository : EfRepository<MoneyOrder>, IMoneyOrderRepository
{
    public EfMoneyOrderRepository(ApexPostDbContext dbContext)
        : base(dbContext)
    {
    }

    public async Task<IReadOnlyCollection<MoneyOrder>> GetByBranchAsync(BranchCode branchCode, CancellationToken cancellationToken = default)
        => await DbContext.MoneyOrders.AsNoTracking().Where(order => order.BranchCode == branchCode).ToListAsync(cancellationToken);

    public async Task<IReadOnlyCollection<MoneyOrder>> GetByBranchAndDateAsync(BranchCode branchCode, DateTime date, CancellationToken cancellationToken = default)
    {
        var start = date.Date;
        var end = start.AddDays(1).AddTicks(-1);
        return await DbContext.MoneyOrders.AsNoTracking()
            .Where(order => order.BranchCode == branchCode && order.CreatedAtUtc >= start && order.CreatedAtUtc <= end)
            .ToListAsync(cancellationToken);
    }
}

public sealed class EfPaymentRepository : EfRepository<Payment>, IPaymentRepository
{
    public EfPaymentRepository(ApexPostDbContext dbContext)
        : base(dbContext)
    {
    }
}

/// <summary>
/// Writes audit rows with its OWN short-lived DbContext (own connection, own SaveChanges), so an audit
/// failure can never break the business transaction and a rolled-back business action still leaves an
/// audit trail. Failures are logged, not thrown. See ADR-014.
/// </summary>
public sealed class EfAuditStore : IAuditStore
{
    private readonly DbContextOptions<ApexPostDbContext> _options;
    private readonly ILogger<EfAuditStore> _logger;

    public EfAuditStore(DbContextOptions<ApexPostDbContext> options, ILogger<EfAuditStore> logger)
    {
        _options = options;
        _logger = logger;
    }

    public async Task AppendAsync(AuditEntry entry, CancellationToken cancellationToken = default)
    {
        try
        {
            await using var auditContext = new ApexPostDbContext(_options);
            auditContext.AuditEntries.Add(new AuditEntryEntity
            {
                Id = entry.Id,
                StaffId = entry.StaffId,
                Role = entry.Role,
                BranchCode = entry.BranchCode,
                Controller = entry.Controller,
                Action = entry.Action,
                CorrelationId = entry.CorrelationId,
                ResultStatus = entry.ResultStatus,
                RouteValues = entry.RouteValues,
                DurationMs = entry.DurationMs,
                RecordedAtUtc = entry.RecordedAtUtc,
                Details = entry.Details,
                RedactedFields = string.Join(';', entry.RedactedFields),
                TransactionId = entry.TransactionId,
                Reason = entry.Reason
            });

            await auditContext.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Audit write failed for {Controller}.{Action} (correlation {CorrelationId}).", entry.Controller, entry.Action, entry.CorrelationId);
        }
    }

    public async Task<IReadOnlyCollection<AuditEntry>> ReadAsync(CancellationToken cancellationToken = default)
    {
        await using var readContext = new ApexPostDbContext(_options);
        var rows = await readContext.AuditEntries.AsNoTracking().ToListAsync(cancellationToken);
        return rows.Select(row => new AuditEntry(
            row.Id,
            row.StaffId,
            row.Role,
            row.BranchCode,
            row.Controller,
            row.Action,
            row.CorrelationId,
            row.ResultStatus,
            row.RouteValues,
            row.DurationMs,
            row.RecordedAtUtc,
            row.Details,
            string.IsNullOrWhiteSpace(row.RedactedFields) ? Array.Empty<string>() : row.RedactedFields.Split(';', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries),
            row.TransactionId,
            row.Reason)).ToList();
    }
}
