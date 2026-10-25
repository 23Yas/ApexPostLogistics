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
            TaxCategory.Standard => amount * 0.2m,
            TaxCategory.Reduced => amount * 0.1m,
            TaxCategory.Exempt => 0m,
            _ => 0m
        };
}
