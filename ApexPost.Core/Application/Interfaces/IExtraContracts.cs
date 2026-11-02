namespace ApexPost.Core.Application.Interfaces;

using ApexPost.Core.Domain;

// ---- Write-side repositories (stage only, never SaveChanges) -----------------------------------

public interface IPaymentRepository : IRepository<Payment>
{
}

/// <summary>Append-only. The implementation maps to a separate persistence record that stores only the last 4 characters.</summary>
public interface IIdentityVerificationRepository
{
    Task AddAsync(IdentityVerification verification, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<IdentityVerification>> GetByTransactionAsync(Guid transactionId, CancellationToken cancellationToken = default);
}

// ---- Query side (read models; AsNoTracking projections or stored procedures) --------------------

public sealed class DailyBranchSalesRow
{
    public string BranchCode { get; init; } = string.Empty;
    public DateTime SaleDate { get; init; }
    public decimal RetailTotal { get; init; }
    public decimal ServiceTotal { get; init; }
}

public sealed class LowStockRow
{
    public string BranchCode { get; init; } = string.Empty;
    public string ProductSku { get; init; } = string.Empty;
    public int QuantityOnHand { get; init; }
    public int ReorderThreshold { get; init; }
    public string ProductName { get; init; } = string.Empty;
}

public sealed record ShippingVolumeRow(string ZoneCode, string ServiceLevel, int ShipmentCount, decimal TotalValue);

public sealed record BranchMoneyOrderRow(string BranchCode, string BranchName, int MoneyOrderCount, int VerifiedIdentityCount);

public interface IReportQueryService
{
    /// <summary>usp_DailyBranchSales. Returns a zero row when the branch has no sales that day.</summary>
    Task<DailyBranchSalesRow> GetDailyBranchSalesAsync(BranchCode branchCode, DateOnly date, CancellationToken cancellationToken = default);

    /// <summary>usp_LowStockReport. branchCode == null means all branches.</summary>
    Task<IReadOnlyCollection<LowStockRow>> GetLowStockAsync(BranchCode? branchCode, int threshold, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<ShippingVolumeRow>> GetShippingVolumeAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<BranchMoneyOrderRow>> GetMoneyOrdersPerBranchAsync(CancellationToken cancellationToken = default);
}

/// <summary>usp_AdjustStock: a transactional stock movement that also writes a StockAdjustments history row.</summary>
public interface IStockAdjustmentService
{
    Task AdjustAsync(BranchCode branchCode, Sku sku, int delta, string reason, string changedBy, CancellationToken cancellationToken = default);
}
