namespace ApexPost.Core.Application.Services;

using ApexPost.Core.Domain;

public static class BusinessRules
{
    public delegate bool StockInventoryRule(StockItem stock, int requiredQuantity);
    public delegate decimal TaxRule(decimal amount);

    public static readonly StockInventoryRule InStockRule = (stock, requiredQuantity) => stock.QuantityOnHand >= requiredQuantity;
    public static readonly Predicate<MoneyOrder> HighValueMoneyOrderRule = moneyOrder => moneyOrder.Amount.Amount > 1500m;
    public static readonly Func<decimal, TaxCategory, decimal> TaxPolicy = ApplyTax;

    public static bool CanFulfillOrder(StockItem stock, int requiredQuantity)
        => InStockRule(stock, requiredQuantity);

    public static bool RequiresIdentityVerification(MoneyOrder moneyOrder)
        => HighValueMoneyOrderRule(moneyOrder);

    public static bool IsIdentityVerificationSatisfied(MoneyOrder moneyOrder, IdentityVerification? verification)
        => !RequiresIdentityVerification(moneyOrder) || (verification is not null && verification.IsVerified);

    public static bool IsParcelWithinWeightLimit(ShippingQuote quote, decimal maxWeight)
        => quote.Weight.ValueKg <= maxWeight;

    public static bool IsStaffAllowedAtBranch(Staff staff, Branch branch)
        => staff.IsActive && branch.IsActive && staff.HomeBranch == branch.Code;

    public static bool IsVoidAllowed(RoleType role) => role == RoleType.BranchManager;

    public static bool IsRefundAllowed(RoleType role) => role == RoleType.BranchManager;

    public static bool IsBranchOpen(Branch branch) => branch.IsActive;

    public static decimal ApplyTax(decimal amount, TaxCategory taxCategory)
        => taxCategory switch
        {
            TaxCategory.Standard => amount * 0.15m,
            TaxCategory.Reduced => amount * 0.05m,
            TaxCategory.Exempt => 0m,
            _ => 0m
        };
}

/// <summary>Shipment lifecycle statuses shared by the API and the courier sync flow.</summary>
public static class ShipmentStatusRules
{
    public static readonly IReadOnlySet<string> KnownStatuses = new HashSet<string>(StringComparer.Ordinal)
    {
        "Created", "PickedUp", "InTransit", "OutForDelivery", "Delivered", "Failed"
    };

    private static readonly IReadOnlyDictionary<string, string[]> AllowedTransitions =
        new Dictionary<string, string[]>(StringComparer.Ordinal)
        {
            ["Created"] = new[] { "PickedUp" },
            ["PickedUp"] = new[] { "InTransit" },
            ["InTransit"] = new[] { "OutForDelivery" },
            ["OutForDelivery"] = new[] { "Delivered", "Failed" },
            ["Delivered"] = Array.Empty<string>(),
            ["Failed"] = Array.Empty<string>()
        };

    public static bool IsKnownStatus(string? status)
        => status is not null && KnownStatuses.Contains(status);

    /// <summary>Whether a shipment may move from <paramref name="currentStatus"/> to <paramref name="newStatus"/>.</summary>
    public static bool CanTransition(string? currentStatus, string? newStatus)
        => currentStatus is not null
           && newStatus is not null
           && AllowedTransitions.TryGetValue(currentStatus, out var allowed)
           && allowed.Contains(newStatus);

    /// <summary>Only shipments that were never picked up may be deleted.</summary>
    public static bool CanDelete(string? status) => status == "Created";
}
