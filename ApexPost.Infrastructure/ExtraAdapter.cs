namespace ApexPost.Infrastructure;

using ApexPost.Core.Application.Interfaces;
using ApexPost.Core.Domain;
using ApexPost.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

public sealed class EfIdentityVerificationRepository : IIdentityVerificationRepository
{
    private readonly ApexPostDbContext _dbContext;

    public EfIdentityVerificationRepository(ApexPostDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    // Stages only. Only the last four characters reach the database (see IdentityVerificationMapper).
    public Task AddAsync(IdentityVerification verification, CancellationToken cancellationToken = default)
    {
        _dbContext.IdentityVerifications.Add(IdentityVerificationMapper.ToRecord(verification));
        return Task.CompletedTask;
    }

    public async Task<IReadOnlyCollection<IdentityVerification>> GetByTransactionAsync(Guid transactionId, CancellationToken cancellationToken = default)
    {
        var rows = await _dbContext.IdentityVerifications
            .AsNoTracking()
            .Where(row => row.TransactionId == transactionId)
            .ToListAsync(cancellationToken);

        return rows.Select(IdentityVerificationMapper.ToDomain).ToList();
    }
}

/// <summary>
/// Stored-procedure and LINQ reports. Procedures are called only through interpolated SQL, which EF sends as
/// parameters (never string concatenation). A procedure cannot be composed with LINQ operators such as
/// FirstOrDefaultAsync, so the procedure call is materialised with ToListAsync first.
/// </summary>
public sealed class EfReportQueryService : IReportQueryService
{
    private readonly ApexPostDbContext _dbContext;

    public EfReportQueryService(ApexPostDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<DailyBranchSalesRow> GetDailyBranchSalesAsync(BranchCode branchCode, DateOnly date, CancellationToken cancellationToken = default)
    {
        var branch = branchCode.Value;
        var day = date.ToDateTime(TimeOnly.MinValue);

        var rows = await _dbContext.Database
            .SqlQuery<DailyBranchSalesRow>($"EXEC dbo.usp_DailyBranchSales @BranchCode = {branch}, @Date = {day}")
            .ToListAsync(cancellationToken);

        return rows.FirstOrDefault() ?? new DailyBranchSalesRow { BranchCode = branch, SaleDate = day };
    }

    public async Task<IReadOnlyCollection<LowStockRow>> GetLowStockAsync(BranchCode? branchCode, int threshold, CancellationToken cancellationToken = default)
    {
        var branch = branchCode?.Value;   // null means "all branches"; EF sends it as NULL

        return await _dbContext.Database
            .SqlQuery<LowStockRow>($"EXEC dbo.usp_LowStockReport @BranchCode = {branch}, @Threshold = {threshold}")
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<ShippingVolumeRow>> GetShippingVolumeAsync(CancellationToken cancellationToken = default)
    {
        var grouped = await (
                from shipment in _dbContext.Shipments.AsNoTracking()
                join zone in _dbContext.ShippingZones.AsNoTracking() on shipment.ZoneId equals zone.Id
                group shipment by new { zone.Code, shipment.ServiceLevel } into g
                select new { g.Key.Code, g.Key.ServiceLevel, Count = g.Count(), Total = g.Sum(x => x.Price) })
            .ToListAsync(cancellationToken);

        return grouped
            .OrderBy(x => x.Code).ThenBy(x => x.ServiceLevel)
            .Select(x => new ShippingVolumeRow(x.Code, x.ServiceLevel.ToString(), x.Count, x.Total))
            .ToList();
    }

    public async Task<IReadOnlyCollection<BranchMoneyOrderRow>> GetMoneyOrdersPerBranchAsync(CancellationToken cancellationToken = default)
    {
        var rows = await _dbContext.Branches.AsNoTracking()
            .Select(branch => new
            {
                Code = branch.Code,
                branch.Name,
                MoneyOrders = _dbContext.MoneyOrders.Count(mo => mo.BranchCode == branch.Code),
                Verified = _dbContext.IdentityVerifications.Count(iv => iv.BranchCode == branch.Code && iv.IsVerified)
            })
            .ToListAsync(cancellationToken);

        return rows
            .OrderBy(row => row.Code.Value)
            .Select(row => new BranchMoneyOrderRow(row.Code.Value, row.Name, row.MoneyOrders, row.Verified))
            .ToList();
    }
}

/// <summary>usp_AdjustStock runs its own transaction and writes the StockAdjustments history row.</summary>
public sealed class EfStockAdjustmentService : IStockAdjustmentService
{
    private readonly ApexPostDbContext _dbContext;

    public EfStockAdjustmentService(ApexPostDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AdjustAsync(BranchCode branchCode, Sku sku, int delta, string reason, string changedBy, CancellationToken cancellationToken = default)
    {
        await _dbContext.Database.ExecuteSqlAsync(
            $"EXEC dbo.usp_AdjustStock @BranchCode = {branchCode.Value}, @Sku = {sku.Value}, @Delta = {delta}, @Reason = {reason}, @ChangedBy = {changedBy}",
            cancellationToken);
    }
}
