namespace ApexPost.Core.Domain;

// ---- Lookup tables (Roles, ProductCategories) -------------------------------------------------
// Staff.Role / Product.Category stay enums in the domain; the tables give the database a real FK target.

public sealed class RoleRecord
{
    public RoleType Id { get; init; }
    public string DisplayName { get; init; } = string.Empty;
}

public sealed class ProductCategoryRecord
{
    public ProductCategory Id { get; init; }
    public string DisplayName { get; init; } = string.Empty;
}

// ---- Counters (was a JSON column on Branches) -------------------------------------------------

public sealed class BranchCounter
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public BranchCode BranchCode { get; init; }
    public string CounterCode { get; init; } = string.Empty;
}

// ---- Stock history (written by usp_AdjustStock) ------------------------------------------------

public sealed class StockAdjustment
{
    public int Id { get; init; }
    public BranchCode BranchCode { get; init; }
    public Sku ProductSku { get; init; }
    public int Delta { get; init; }
    public string Reason { get; init; } = string.Empty;
    public string ChangedBy { get; init; } = string.Empty;
    public DateTimeOffset CreatedAtUtc { get; init; } = DateTimeOffset.UtcNow;
}

// ---- Shipping ---------------------------------------------------------------------------------
// Money columns on the new tables are plain decimals on purpose: EF cannot translate arithmetic
// (SUM, +, *) through a value-converted struct, and these tables are used by reports.

public sealed class ShippingZone
{
    public int Id { get; init; }
    public string Code { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Region { get; init; } = string.Empty;
}

public sealed class ShippingRate
{
    public int Id { get; init; }
    public int ZoneId { get; init; }
    public ServiceLevel ServiceLevel { get; init; }
    public decimal MaxWeightKg { get; init; }
    public decimal BaseRate { get; init; }
    public decimal PerKgRate { get; init; }
}

public sealed class Shipment
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid CounterTransactionId { get; init; }
    public BranchCode BranchCode { get; init; }
    public int ZoneId { get; init; }
    public ServiceLevel ServiceLevel { get; init; }
    public decimal WeightKg { get; set; }
    public decimal Price { get; set; }
    public string TrackingReference { get; init; } = string.Empty;
    public string Status { get; set; } = "Created";
    public DateTimeOffset CreatedAtUtc { get; init; } = DateTimeOffset.UtcNow;
}

// ---- Payments ---------------------------------------------------------------------------------

public sealed class Payment
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid CounterTransactionId { get; init; }
    public PaymentMethod Method { get; init; }
    public decimal Amount { get; init; }
    public string Status { get; init; } = "Captured";
    public DateTimeOffset CreatedAtUtc { get; init; } = DateTimeOffset.UtcNow;
}

// ---- Identity documents: masked only, enforced in the domain ------------------------------------

public static class MaskedIdentifierPolicy
{
    public const int MinLength = 4;
    public const int MaxLength = 20;

    /// <summary>A masked identifier must contain at least one non-digit (the mask) and be 4-20 chars.</summary>
    public static bool IsValid(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var trimmed = value.Trim();
        if (trimmed.All(char.IsDigit))
        {
            return false; // looks like a full document number
        }

        return trimmed.Length is >= MinLength and <= MaxLength;
    }

    /// <summary>Only the last four characters are ever persisted.</summary>
    public static string LastFour(string value)
    {
        var trimmed = value.Trim();
        return trimmed.Length <= 4 ? trimmed : trimmed[^4..];
    }
}
