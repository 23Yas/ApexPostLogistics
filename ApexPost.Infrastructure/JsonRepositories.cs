namespace ApexPost.Infrastructure;

using System.Collections.Concurrent;
using System.Text.Json;
using ApexPost.Core.Application.Interfaces;
using ApexPost.Core.Application.Services;
using ApexPost.Core.Domain;

public abstract class JsonRepositoryBase<T> : IRepository<T> where T : class
{
    private static readonly SemaphoreSlim WriteLock = new(1, 1);
    private readonly string _filePath;

    protected JsonRepositoryBase(string filePath)
    {
        _filePath = filePath;
        Directory.CreateDirectory(Path.GetDirectoryName(_filePath) ?? AppContext.BaseDirectory);
    }

    public async Task<IReadOnlyCollection<T>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var items = await LoadAsync(cancellationToken);
        return items;
    }

    public async Task<T?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var items = await LoadAsync(cancellationToken);
        return items.FirstOrDefault(item => GetId(item) == id);
    }

    public async Task AddAsync(T entity, CancellationToken cancellationToken = default)
    {
        var items = await LoadAsync(cancellationToken);
        items.Add(entity);
        await SaveAsync(items, cancellationToken);
    }

    public async Task UpdateAsync(T entity, CancellationToken cancellationToken = default)
    {
        var items = await LoadAsync(cancellationToken);
        for (var i = 0; i < items.Count; i++)
        {
            if (GetId(items[i]) == GetId(entity))
            {
                items[i] = entity;
                await SaveAsync(items, cancellationToken);
                return;
            }
        }

        throw new InvalidOperationException("Entity not found.");
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var items = await LoadAsync(cancellationToken);
        var index = items.FindIndex(item => GetId(item) == id);
        if (index >= 0)
        {
            items.RemoveAt(index);
            await SaveAsync(items, cancellationToken);
        }
    }

    protected abstract Guid GetId(T item);

    protected async Task<List<T>> LoadAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!File.Exists(_filePath))
        {
            return [];
        }

        var json = await File.ReadAllTextAsync(_filePath, cancellationToken);
        if (string.IsNullOrWhiteSpace(json))
        {
            return [];
        }

        var items = JsonSerializer.Deserialize<List<T>>(json);
        return items ?? [];
    }

    protected async Task SaveAsync(List<T> items, CancellationToken cancellationToken)
    {
        await WriteLock.WaitAsync(cancellationToken);
        try
        {
            var json = JsonSerializer.Serialize(items, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(_filePath, json, cancellationToken);
        }
        finally
        {
            WriteLock.Release();
        }
    }
}

public sealed class BranchRepository : JsonRepositoryBase<Branch>, IBranchRepository
{
    public BranchRepository(string filePath) : base(filePath)
    {
    }

    public async Task<Branch?> GetByCodeAsync(BranchCode code, CancellationToken cancellationToken = default)
    {
        var branches = await GetAllAsync(cancellationToken);
        return branches.FirstOrDefault(branch => branch.Code == code);
    }

    protected override Guid GetId(Branch item) => item.Id;
}

public sealed class StaffRepository : JsonRepositoryBase<Staff>, IStaffRepository
{
    public StaffRepository(string filePath) : base(filePath)
    {
    }

    public async Task<Staff?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        var staff = await GetAllAsync(cancellationToken);
        return staff.FirstOrDefault(item => item.Email.Equals(email, StringComparison.OrdinalIgnoreCase));
    }

    protected override Guid GetId(Staff item) => item.Id;
}

public sealed class ProductRepository : JsonRepositoryBase<Product>, IProductRepository
{
    private readonly ConcurrentDictionary<string, IReadOnlyCollection<Product>> _cache = new();

    public ProductRepository(string filePath) : base(filePath)
    {
    }

    public async Task<Product?> GetBySkuAsync(Sku sku, CancellationToken cancellationToken = default)
    {
        var products = await GetAllAsync(cancellationToken);
        return products.FirstOrDefault(product => product.Sku == sku);
    }

    public ValueTask<IReadOnlyCollection<Product>> GetCachedProductsAsync(CancellationToken cancellationToken = default)
    {
        if (_cache.TryGetValue("all", out var cached))
        {
            return ValueTask.FromResult(cached);
        }

        return new ValueTask<IReadOnlyCollection<Product>>(PopulateCacheAsync(cancellationToken));
    }

    private async Task<IReadOnlyCollection<Product>> PopulateCacheAsync(CancellationToken cancellationToken)
    {
        var products = await GetAllAsync(cancellationToken);
        _cache["all"] = products;
        return products;
    }

    protected override Guid GetId(Product item) => item.Id;
}

public sealed class StockRepository : JsonRepositoryBase<StockItem>, IStockRepository
{
    public StockRepository(string filePath) : base(filePath)
    {
    }

    public async Task<StockItem?> GetByBranchAndSkuAsync(BranchCode branchCode, Sku sku, CancellationToken cancellationToken = default)
    {
        var items = await GetAllAsync(cancellationToken);
        return items.FirstOrDefault(item => item.BranchCode == branchCode && item.ProductSku == sku);
    }

    public async Task<IReadOnlyCollection<StockItem>> GetLowStockAsync(BranchCode branchCode, int threshold, CancellationToken cancellationToken = default)
    {
        var items = await GetAllAsync(cancellationToken);
        return items.Where(item => item.BranchCode == branchCode && item.QuantityOnHand <= threshold)
            .ToList();
    }

    protected override Guid GetId(StockItem item) => item.Id;
}

public sealed class TransactionRepository : JsonRepositoryBase<CounterTransaction>, ITransactionRepository
{
    public TransactionRepository(string filePath) : base(filePath)
    {
    }

    public async Task<IReadOnlyCollection<CounterTransaction>> GetByBranchAsync(BranchCode branchCode, CancellationToken cancellationToken = default)
    {
        var transactions = await GetAllAsync(cancellationToken);
        return transactions.Where(t => t.BranchCode == branchCode).ToList();
    }

    protected override Guid GetId(CounterTransaction item) => item.Id;
}

public sealed class JsonAuditStore : IAuditStore
{
    private static readonly SemaphoreSlim WriteLock = new(1, 1);
    private readonly string _filePath;

    public JsonAuditStore(string filePath)
    {
        _filePath = filePath;
        Directory.CreateDirectory(Path.GetDirectoryName(_filePath) ?? AppContext.BaseDirectory);
    }

    public async Task AppendAsync(AuditEntry entry, CancellationToken cancellationToken = default)
    {
        await WriteLock.WaitAsync(cancellationToken);
        try
        {
            var entries = await ReadAsync(cancellationToken);
            var list = entries.ToList();
            list.Add(entry);
            var json = JsonSerializer.Serialize(list, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(_filePath, json, cancellationToken);
        }
        finally
        {
            WriteLock.Release();
        }
    }

    public async Task<IReadOnlyCollection<AuditEntry>> ReadAsync(CancellationToken cancellationToken = default)
    {
        if (!File.Exists(_filePath))
        {
            return [];
        }

        var json = await File.ReadAllTextAsync(_filePath, cancellationToken);
        if (string.IsNullOrWhiteSpace(json))
        {
            return [];
        }

        var entries = JsonSerializer.Deserialize<List<AuditEntry>>(json);
        return entries ?? [];
    }
}

public sealed class SimulatedNotificationSink : INotificationSink
{
    private readonly List<DomainEvent> _events = [];

    public Task PublishAsync(DomainEvent domainEvent, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _events.Add(domainEvent);
        return Task.CompletedTask;
    }

    public IReadOnlyCollection<DomainEvent> Events => _events;
}

public sealed class CourierRateProvider : IShippingRateProvider
{
    private static readonly SemaphoreSlim RateLock = new(1, 1);
    private readonly Dictionary<string, decimal> _zoneRates = new(StringComparer.OrdinalIgnoreCase)
    {
        ["North"] = 1.20m,
        ["South"] = 1.10m,
        ["East"] = 1.35m,
        ["West"] = 1.25m,
        ["Central"] = 1.15m,
        ["Midlands"] = 1.18m,
        ["Scotland"] = 1.42m,
        ["Wales"] = 1.28m,
        ["Northern Ireland"] = 1.48m,
        ["Channel Islands"] = 1.55m
    };

    public async Task<decimal> GetRateAsync(string zone, ServiceLevel serviceLevel, bool isLetter, CancellationToken cancellationToken = default)
    {
        await RateLock.WaitAsync(cancellationToken);
        try
        {
            var multiplier = serviceLevel switch
            {
                ServiceLevel.Standard => 1m,
                ServiceLevel.Express => 1.4m,
                ServiceLevel.Registered => 1.8m,
                _ => 1m
            };

            var baseRate = _zoneRates.TryGetValue(zone, out var rate) ? rate : 1.20m;
            return isLetter ? baseRate * multiplier * 0.8m : baseRate * multiplier;
        }
        finally
        {
            RateLock.Release();
        }
    }
}
