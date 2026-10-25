namespace ApexPost.Core.Application.Models;

using ApexPost.Core.Domain;

public sealed record BranchDashboard(
    Branch Branch,
    IReadOnlyCollection<StockItem> LowStock,
    IReadOnlyCollection<CounterTransaction> Transactions,
    decimal SalesToday,
    decimal ServiceSales,
    decimal RetailSales);

public sealed record ShippingLabelPreview(
    string TrackingReference,
    string Zone,
    ServiceLevel ServiceLevel,
    Weight Weight,
    string FromAddress,
    string ToAddress,
    Money Price);
