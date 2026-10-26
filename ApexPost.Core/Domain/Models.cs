namespace ApexPost.Core.Domain;

public enum RoleType
{
    CounterClerk,
    BranchManager,
    Auditor,
    Administrator
}

public enum ProductCategory
{
    Packaging,
    Stationery,
    OfficeSupplies
}

public enum TaxCategory
{
    Standard,
    Reduced,
    Exempt
}

public enum ServiceLevel
{
    Standard,
    Express,
    Registered
}

public enum TransactionStatus
{
    Open,
    Paid,
    Voided,
    Refunded
}

public enum PaymentMethod
{
    Cash,
    Card,
    Mobile,
    MoneyOrder
}

public readonly record struct BranchCode(string Value)
{
    public override string ToString() => Value;
}

public readonly record struct Sku(string Value)
{
    public override string ToString() => Value;
}

public readonly record struct Money(decimal Amount)
{
    public static Money Zero => new(0m);

    public decimal Rounded => decimal.Round(Amount, 2, MidpointRounding.AwayFromZero);

    public static Money operator +(Money left, Money right) => new(left.Amount + right.Amount);

    public static Money operator -(Money left, Money right) => new(left.Amount - right.Amount);

    public static Money operator *(Money left, decimal scalar) => new(left.Amount * scalar);

    public static Money operator /(Money left, decimal divisor) => new(left.Amount / divisor);

    public static implicit operator decimal(Money money) => money.Amount;

    public static implicit operator Money(decimal amount) => new(amount);
}

public readonly record struct Weight(decimal ValueKg)
{
    public decimal RoundedKg => decimal.Round(ValueKg, 2, MidpointRounding.AwayFromZero);

    public static implicit operator decimal(Weight weight) => weight.ValueKg;

    public static implicit operator Weight(decimal kilograms) => new(kilograms);
}

public readonly record struct Address(string Line1, string City, string Postcode)
{
    public string Summary => $"{Line1}, {City} {Postcode}";
}

public sealed class Branch
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public BranchCode Code { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Region { get; init; } = string.Empty;
    public string OpeningHours { get; init; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public List<string> Counters { get; init; } = [];
}

public sealed class Staff
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public string Name { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string PasswordHash { get; init; } = string.Empty;
    public RoleType Role { get; init; }
    public BranchCode HomeBranch { get; init; }
    public bool IsActive { get; set; } = true;
}

public sealed class Product
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Sku Sku { get; init; }
    public string Name { get; init; } = string.Empty;
    public ProductCategory Category { get; init; }
    public Money UnitPrice { get; init; }
    public TaxCategory TaxCategory { get; init; }
}

public sealed class StockItem
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public BranchCode BranchCode { get; init; }
    public Sku ProductSku { get; init; }
    public int QuantityOnHand { get; set; }
    public int ReorderThreshold { get; init; } = 5;
}

public sealed class IdentityVerification
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid StaffId { get; init; }
    public Guid TransactionId { get; init; }
    public BranchCode BranchCode { get; init; }
    public string DocumentType { get; init; } = string.Empty;
    public bool IsVerified { get; init; }
    public string MaskedIdentifier { get; init; } = string.Empty;
    public DateTimeOffset VerifiedAtUtc { get; init; }
}

public sealed class MoneyOrder
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid TransactionId { get; init; }
    public BranchCode BranchCode { get; init; }
    public Money Amount { get; init; }
    public string Sender { get; init; } = string.Empty;
    public string Recipient { get; init; } = string.Empty;
    public Money Fee { get; init; }
    public bool RequiresIdentityVerification { get; init; }
    public bool IdentityVerificationComplete { get; init; }
    public string Status { get; init; } = "Pending";
}

public sealed class ShippingQuote
{
    public string Zone { get; init; } = string.Empty;
    public ServiceLevel ServiceLevel { get; init; }
    public Weight Weight { get; init; }
    public bool IsLetter { get; init; }
}

public sealed class TransactionLine
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public string Description { get; init; } = string.Empty;
    public Sku? ProductSku { get; init; }
    public string? ServiceCode { get; init; }
    public int Quantity { get; init; } = 1;
    public Money UnitPrice { get; init; }
    public TaxCategory TaxCategory { get; init; }
    public bool IsRetailItem => ProductSku is not null;
    public bool IsService => ServiceCode is not null;
}

public sealed class CounterTransaction
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public BranchCode BranchCode { get; init; }
    public string CounterId { get; init; } = string.Empty;
    public Guid ClerkId { get; init; }
    public List<TransactionLine> LineItems { get; init; } = [];
    public PaymentMethod PaymentMethod { get; init; }
    public TransactionStatus Status { get; set; } = TransactionStatus.Open;
    public Money SubTotal { get; set; }
    public Money TaxTotal { get; set; }
    public Money Total { get; set; }
    public DateTimeOffset CreatedAtUtc { get; init; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? ClosedAtUtc { get; set; }
    public string? VoidReason { get; set; }
    public string? AuditCorrelationId { get; set; }
}

public sealed class DomainEvent
{
    public string EventType { get; init; } = string.Empty;
    public DateTimeOffset OccurredAtUtc { get; init; } = DateTimeOffset.UtcNow;
    public string Message { get; init; } = string.Empty;
    public BranchCode BranchCode { get; init; }
}
