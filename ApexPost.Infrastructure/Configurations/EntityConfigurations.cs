namespace ApexPost.Infrastructure.Configurations;

using ApexPost.Core.Domain;
using ApexPost.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

// One IEntityTypeConfiguration per entity. Applied by ApexPostDbContext via ApplyConfigurationsFromAssembly.
// Delete behaviour: Restrict everywhere (history must never vanish) except the owned TransactionLines,
// which cascade with their parent transaction. AuditEntries has no FKs on purpose (append-only, must
// survive anything). The reasons are written up in ADR-008.

internal static class ConfigurationHelpers
{
    public static PropertyBuilder<BranchCode> AsBranchCode(this PropertyBuilder<BranchCode> property)
        => property.HasConversion(ApexPostValueConverters.BranchCodeConverter).HasMaxLength(32).IsUnicode(false).IsRequired();

    public static PropertyBuilder<Sku> AsSku(this PropertyBuilder<Sku> property)
        => property.HasConversion(ApexPostValueConverters.SkuConverter).HasMaxLength(64).IsUnicode(false).IsRequired();

    public static PropertyBuilder<Money> AsMoney(this PropertyBuilder<Money> property)
        => property.HasConversion(ApexPostValueConverters.MoneyConverter).HasColumnType("decimal(18,2)");

    /// <summary>CreatedAtUtc / CreatedBy / ModifiedAtUtc / ModifiedBy. Works for CLR properties and shadow properties alike.</summary>
    public static void WithAuditColumns<T>(this EntityTypeBuilder<T> entity) where T : class
    {
        // Most entities already have CLR CreatedAtUtc/ModifiedAtUtc; a shadow property with the same name would clash.
        if (typeof(T).GetProperty("CreatedAtUtc") is null)
            entity.Property<DateTimeOffset>("CreatedAtUtc").HasDefaultValueSql("SYSUTCDATETIME()");
        else
            entity.Property("CreatedAtUtc").HasDefaultValueSql("SYSUTCDATETIME()");

        if (typeof(T).GetProperty("ModifiedAtUtc") is null)
            entity.Property<DateTimeOffset?>("ModifiedAtUtc");

        entity.Property<string>("CreatedBy").HasMaxLength(128).HasDefaultValue("system");
        entity.Property<string>("ModifiedBy").HasMaxLength(128);
    }
}

public sealed class RoleRecordConfiguration : IEntityTypeConfiguration<RoleRecord>
{
    public void Configure(EntityTypeBuilder<RoleRecord> entity)
    {
        entity.ToTable("Roles");
        entity.HasKey(x => x.Id);
        entity.Property(x => x.Id).HasConversion<string>().HasMaxLength(32).IsUnicode(false).ValueGeneratedNever();
        entity.Property(x => x.DisplayName).HasMaxLength(100).IsRequired();
        entity.HasData(Enum.GetValues<RoleType>().Select(role => new RoleRecord { Id = role, DisplayName = role.ToString() }));
    }
}

public sealed class ProductCategoryRecordConfiguration : IEntityTypeConfiguration<ProductCategoryRecord>
{
    public void Configure(EntityTypeBuilder<ProductCategoryRecord> entity)
    {
        entity.ToTable("ProductCategories");
        entity.HasKey(x => x.Id);
        entity.Property(x => x.Id).HasConversion<string>().HasMaxLength(32).IsUnicode(false).ValueGeneratedNever();
        entity.Property(x => x.DisplayName).HasMaxLength(100).IsRequired();
        entity.HasData(Enum.GetValues<ProductCategory>().Select(category => new ProductCategoryRecord { Id = category, DisplayName = category.ToString() }));
    }
}

public sealed class BranchConfiguration : IEntityTypeConfiguration<Branch>
{
    public void Configure(EntityTypeBuilder<Branch> entity)
    {
        entity.ToTable("Branches");
        entity.HasKey(x => x.Id);
        entity.Property(x => x.Code).AsBranchCode();
        entity.HasAlternateKey(x => x.Code);                 // unique + usable as an FK target
        entity.Property(x => x.Name).HasMaxLength(200).IsRequired();
        entity.Property(x => x.Region).HasMaxLength(100).IsRequired();
        entity.Property(x => x.OpeningHours).HasMaxLength(100).IsRequired();
        entity.Property(x => x.IsActive).HasDefaultValue(true);
        entity.Ignore(x => x.Counters);                      // counters now live in their own table
        entity.WithAuditColumns();
    }
}

public sealed class BranchCounterConfiguration : IEntityTypeConfiguration<BranchCounter>
{
    public void Configure(EntityTypeBuilder<BranchCounter> entity)
    {
        entity.ToTable("Counters");
        entity.HasKey(x => x.Id);
        entity.Property(x => x.BranchCode).AsBranchCode();
        entity.Property(x => x.CounterCode).HasMaxLength(32).IsUnicode(false).IsRequired();
        entity.HasIndex(x => new { x.BranchCode, x.CounterCode }).IsUnique();
        entity.HasOne<Branch>().WithMany().HasForeignKey(x => x.BranchCode).HasPrincipalKey(b => b.Code).OnDelete(DeleteBehavior.Restrict);
        entity.WithAuditColumns();
    }
}

public sealed class StaffConfiguration : IEntityTypeConfiguration<Staff>
{
    public void Configure(EntityTypeBuilder<Staff> entity)
    {
        entity.ToTable("Staff");
        entity.HasKey(x => x.Id);
        entity.Property(x => x.Name).HasMaxLength(200).IsRequired();
        entity.Property(x => x.Email).HasMaxLength(200).IsRequired();
        entity.HasIndex(x => x.Email).IsUnique();
        entity.Property(x => x.PasswordHash).HasMaxLength(512).IsRequired();
        entity.Property(x => x.Role).HasConversion<string>().HasMaxLength(32).IsUnicode(false).IsRequired();
        entity.Property(x => x.HomeBranch).AsBranchCode();
        entity.Property(x => x.IsActive).HasDefaultValue(true);
        entity.HasOne<RoleRecord>().WithMany().HasForeignKey(x => x.Role).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne<Branch>().WithMany().HasForeignKey(x => x.HomeBranch).HasPrincipalKey(b => b.Code).OnDelete(DeleteBehavior.Restrict);
        entity.WithAuditColumns();
    }
}

public sealed class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> entity)
    {
        entity.ToTable("Products", table => table.HasCheckConstraint("CK_Products_UnitPrice_NonNegative", "UnitPrice >= 0"));
        entity.HasKey(x => x.Id);
        entity.Property(x => x.Sku).AsSku();
        entity.HasAlternateKey(x => x.Sku);
        entity.Property(x => x.Name).HasMaxLength(200).IsRequired();
        entity.Property(x => x.Category).HasConversion<string>().HasMaxLength(32).IsUnicode(false).IsRequired();
        entity.Property(x => x.UnitPrice).AsMoney();
        entity.Property(x => x.TaxCategory).HasConversion<string>().HasMaxLength(32).IsUnicode(false).IsRequired();
        entity.HasOne<ProductCategoryRecord>().WithMany().HasForeignKey(x => x.Category).OnDelete(DeleteBehavior.Restrict);
        entity.WithAuditColumns();
    }
}

public sealed class StockItemConfiguration : IEntityTypeConfiguration<StockItem>
{
    public void Configure(EntityTypeBuilder<StockItem> entity)
    {
        entity.ToTable("BranchStock", table =>
        {
            table.HasCheckConstraint("CK_BranchStock_QuantityOnHand_NonNegative", "QuantityOnHand >= 0");
            table.HasCheckConstraint("CK_BranchStock_ReorderThreshold_NonNegative", "ReorderThreshold >= 0");
        });
        entity.HasKey(x => x.Id);
        entity.Property(x => x.BranchCode).AsBranchCode();
        entity.Property(x => x.ProductSku).AsSku();
        entity.HasIndex(x => new { x.BranchCode, x.ProductSku }).IsUnique();
        entity.Property(x => x.ReorderThreshold).HasDefaultValue(5);
        entity.Property<byte[]>("RowVersion").IsRowVersion();   // optimistic concurrency (ADR-013)
        entity.HasOne<Branch>().WithMany().HasForeignKey(x => x.BranchCode).HasPrincipalKey(b => b.Code).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne<Product>().WithMany().HasForeignKey(x => x.ProductSku).HasPrincipalKey(p => p.Sku).OnDelete(DeleteBehavior.Restrict);
        entity.WithAuditColumns();
    }
}

public sealed class StockAdjustmentConfiguration : IEntityTypeConfiguration<StockAdjustment>
{
    public void Configure(EntityTypeBuilder<StockAdjustment> entity)
    {
        entity.ToTable("StockAdjustments", table => table.HasCheckConstraint("CK_StockAdjustments_Delta_NonZero", "Delta <> 0"));
        entity.HasKey(x => x.Id);
        entity.Property(x => x.Id).ValueGeneratedOnAdd();
        entity.Property(x => x.BranchCode).AsBranchCode();
        entity.Property(x => x.ProductSku).AsSku();
        entity.Property(x => x.Reason).HasMaxLength(200).IsRequired();
        entity.Property(x => x.ChangedBy).HasMaxLength(128).IsRequired();
        entity.HasOne<Branch>().WithMany().HasForeignKey(x => x.BranchCode).HasPrincipalKey(b => b.Code).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne<Product>().WithMany().HasForeignKey(x => x.ProductSku).HasPrincipalKey(p => p.Sku).OnDelete(DeleteBehavior.Restrict);
        entity.WithAuditColumns();
    }
}

public sealed class ShippingZoneConfiguration : IEntityTypeConfiguration<ShippingZone>
{
    public void Configure(EntityTypeBuilder<ShippingZone> entity)
    {
        entity.ToTable("ShippingZones");
        entity.HasKey(x => x.Id);
        entity.Property(x => x.Id).ValueGeneratedNever();
        entity.Property(x => x.Code).HasMaxLength(16).IsUnicode(false).IsRequired();
        entity.HasIndex(x => x.Code).IsUnique();
        entity.Property(x => x.Name).HasMaxLength(100).IsRequired();
        entity.Property(x => x.Region).HasMaxLength(100).IsRequired();
        entity.WithAuditColumns();
    }
}

public sealed class ShippingRateConfiguration : IEntityTypeConfiguration<ShippingRate>
{
    public void Configure(EntityTypeBuilder<ShippingRate> entity)
    {
        entity.ToTable("ShippingRates", table =>
        {
            table.HasCheckConstraint("CK_ShippingRates_MaxWeight_Range", "MaxWeightKg > 0 AND MaxWeightKg <= 30");
            table.HasCheckConstraint("CK_ShippingRates_Rates_NonNegative", "BaseRate >= 0 AND PerKgRate >= 0");
        });
        entity.HasKey(x => x.Id);
        entity.Property(x => x.Id).ValueGeneratedNever();
        entity.Property(x => x.ServiceLevel).HasConversion<string>().HasMaxLength(32).IsUnicode(false).IsRequired();
        entity.Property(x => x.MaxWeightKg).HasColumnType("decimal(8,2)");
        entity.Property(x => x.BaseRate).HasColumnType("decimal(18,2)");
        entity.Property(x => x.PerKgRate).HasColumnType("decimal(18,2)");
        entity.HasIndex(x => new { x.ZoneId, x.ServiceLevel, x.MaxWeightKg }).IsUnique();
        entity.HasOne<ShippingZone>().WithMany().HasForeignKey(x => x.ZoneId).OnDelete(DeleteBehavior.Restrict);
        entity.WithAuditColumns();
    }
}

public sealed class CounterTransactionConfiguration : IEntityTypeConfiguration<CounterTransaction>
{
    public void Configure(EntityTypeBuilder<CounterTransaction> entity)
    {
        entity.ToTable("CounterTransactions", table =>
        {
            table.HasCheckConstraint("CK_CounterTransactions_Total_NonNegative", "Total >= 0");
            table.HasCheckConstraint("CK_CounterTransactions_Status_Valid", "Status IN ('Open','Paid','Voided','Refunded')");
        });
        entity.HasKey(x => x.Id);
        entity.Property(x => x.BranchCode).AsBranchCode();
        entity.Property(x => x.CounterId).HasMaxLength(32).IsUnicode(false).IsRequired();
        entity.Property(x => x.PaymentMethod).HasConversion<string>().HasMaxLength(32).IsUnicode(false).IsRequired();
        entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(32).IsUnicode(false).IsRequired();
        entity.Property(x => x.SubTotal).AsMoney();
        entity.Property(x => x.TaxTotal).AsMoney();
        entity.Property(x => x.Total).AsMoney();
        entity.Property(x => x.ClosedAtUtc).HasColumnType("datetimeoffset");
        entity.Property(x => x.VoidReason).HasMaxLength(200);
        entity.Property(x => x.AuditCorrelationId).HasMaxLength(128);
        entity.Property<byte[]>("RowVersion").IsRowVersion();
        entity.HasOne<Branch>().WithMany().HasForeignKey(x => x.BranchCode).HasPrincipalKey(b => b.Code).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne<Staff>().WithMany().HasForeignKey(x => x.ClerkId).OnDelete(DeleteBehavior.Restrict);
        entity.WithAuditColumns();

        // TransactionLines: dependent detail rows, cascade-deleted with their parent.
        entity.OwnsMany(x => x.LineItems, line =>
        {
            line.ToTable("TransactionLines", table =>
            {
                table.HasCheckConstraint("CK_TransactionLines_Quantity_Positive", "Quantity > 0");
                table.HasCheckConstraint("CK_TransactionLines_UnitPrice_NonNegative", "UnitPrice >= 0");
                table.HasCheckConstraint("CK_TransactionLines_RetailXorService",
                    "(ProductSku IS NOT NULL AND ServiceCode IS NULL) OR (ProductSku IS NULL AND ServiceCode IS NOT NULL)");
            });
            line.WithOwner().HasForeignKey("CounterTransactionId");
            line.HasKey(x => x.Id);
            line.Property(x => x.Id).ValueGeneratedNever();
            line.Property(x => x.Description).HasMaxLength(200).IsRequired();
            line.Property(x => x.ProductSku)
                .HasConversion(v => v.HasValue ? v.Value.Value : null, v => string.IsNullOrWhiteSpace(v) ? null : new Sku(v))
                .HasMaxLength(64).IsUnicode(false);
            line.Property(x => x.ServiceCode).HasMaxLength(64).IsUnicode(false);
            line.Property(x => x.Quantity).IsRequired();
            line.Property(x => x.UnitPrice).AsMoney();
            line.Property(x => x.TaxCategory).HasConversion<string>().HasMaxLength(32).IsUnicode(false).IsRequired();
            line.Ignore(x => x.IsRetailItem);
            line.Ignore(x => x.IsService);
            line.Property(x => x.CreatedAtUtc).HasDefaultValueSql("SYSUTCDATETIME()");
            line.Property(x => x.ModifiedAtUtc);
            line.Property<string>("CreatedBy").HasMaxLength(128).HasDefaultValue("system");
            line.Property<string>("ModifiedBy").HasMaxLength(128);
        });
    }
}

public sealed class PaymentConfiguration : IEntityTypeConfiguration<Payment>
{
    public void Configure(EntityTypeBuilder<Payment> entity)
    {
        entity.ToTable("Payments", table =>
        {
            table.HasCheckConstraint("CK_Payments_Amount_Positive", "Amount > 0");
            table.HasCheckConstraint("CK_Payments_Status_Valid", "Status IN ('Captured','Refunded','Failed')");
        });
        entity.HasKey(x => x.Id);
        entity.Property(x => x.Method).HasConversion<string>().HasMaxLength(32).IsUnicode(false).IsRequired();
        entity.Property(x => x.Amount).HasColumnType("decimal(18,2)");
        entity.Property(x => x.Status).HasMaxLength(32).IsUnicode(false).HasDefaultValue("Captured");
        entity.HasOne<CounterTransaction>().WithMany().HasForeignKey(x => x.CounterTransactionId).OnDelete(DeleteBehavior.Restrict);
        entity.WithAuditColumns();
    }
}

public sealed class ShipmentConfiguration : IEntityTypeConfiguration<Shipment>
{
    public void Configure(EntityTypeBuilder<Shipment> entity)
    {
        entity.ToTable("Shipments", table =>
        {
            table.HasCheckConstraint("CK_Shipments_Weight_Range", "WeightKg > 0 AND WeightKg <= 70");
            table.HasCheckConstraint("CK_Shipments_Price_NonNegative", "Price >= 0");
            table.HasCheckConstraint("CK_Shipments_Status_Valid", "Status IN ('Created','PickedUp','InTransit','OutForDelivery','Delivered','Failed','Returned')");
        });
        entity.HasKey(x => x.Id);
        entity.Property(x => x.BranchCode)
            .HasConversion(ApexPostValueConverters.BranchCodeNullableConverter)
            .HasMaxLength(32).IsUnicode(false);
        entity.Property(x => x.ServiceLevel).HasConversion<string>().HasMaxLength(32).IsUnicode(false).IsRequired();
        entity.Property(x => x.WeightKg).HasColumnType("decimal(8,2)");
        entity.Property(x => x.Price).HasColumnType("decimal(18,2)");
        entity.Property(x => x.TrackingReference).HasMaxLength(40).IsUnicode(false).IsRequired();
        entity.HasIndex(x => x.TrackingReference).IsUnique();
        entity.Property(x => x.Status).HasMaxLength(32).IsUnicode(false).HasDefaultValue("Created");
        entity.Property(x => x.RecipientName).HasMaxLength(200).IsRequired();
        entity.Property(x => x.RecipientPhone).HasMaxLength(32).IsUnicode(false).IsRequired();
        entity.Property(x => x.RecipientAddress).HasMaxLength(400).IsRequired();
        entity.Property(x => x.RecipientCity).HasMaxLength(100).IsRequired();
        entity.HasOne<CounterTransaction>().WithMany().HasForeignKey(x => x.CounterTransactionId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne<Branch>().WithMany().HasForeignKey(x => x.BranchCode).HasPrincipalKey(b => b.Code).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne<ShippingZone>().WithMany().HasForeignKey(x => x.ZoneId).OnDelete(DeleteBehavior.Restrict);
        entity.WithAuditColumns();
    }
}

public sealed class MoneyOrderConfiguration : IEntityTypeConfiguration<MoneyOrder>
{
    public void Configure(EntityTypeBuilder<MoneyOrder> entity)
    {
        entity.ToTable("MoneyOrders", table =>
        {
            table.HasCheckConstraint("CK_MoneyOrders_Amount_Positive", "Amount > 0");
            table.HasCheckConstraint("CK_MoneyOrders_Fee_NonNegative", "Fee >= 0");
            table.HasCheckConstraint("CK_MoneyOrders_Status_Valid", "Status IN ('Pending','Approved','Rejected')");
        });
        entity.HasKey(x => x.Id);
        entity.Property(x => x.TransactionId).IsRequired(false);
        entity.Property(x => x.BranchCode).AsBranchCode();
        entity.Property(x => x.Amount).AsMoney();
        entity.Property(x => x.Fee).AsMoney();
        entity.Property(x => x.Sender).HasMaxLength(200).IsRequired();
        entity.Property(x => x.Recipient).HasMaxLength(200).IsRequired();
        entity.Property(x => x.RequiresIdentityVerification).HasDefaultValue(false);
        entity.Property(x => x.IdentityVerificationComplete).HasDefaultValue(false);
        entity.Property(x => x.Status).HasMaxLength(32).IsUnicode(false).HasDefaultValue("Pending");
        entity.HasOne<CounterTransaction>().WithMany().HasForeignKey(x => x.TransactionId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne<Branch>().WithMany().HasForeignKey(x => x.BranchCode).HasPrincipalKey(b => b.Code).OnDelete(DeleteBehavior.Restrict);
        entity.WithAuditColumns();
    }
}

public sealed class IdentityVerificationRecordConfiguration : IEntityTypeConfiguration<IdentityVerificationRecord>
{
    public void Configure(EntityTypeBuilder<IdentityVerificationRecord> entity)
    {
        entity.ToTable("IdentityVerifications");
        entity.HasKey(x => x.Id);
        entity.Property(x => x.BranchCode).AsBranchCode();
        entity.Property(x => x.DocumentType).HasMaxLength(64).IsRequired();
        entity.Property(x => x.IsVerified).HasDefaultValue(false);
        entity.Property(x => x.MaskedLast4).HasMaxLength(4).IsUnicode(false).IsRequired();   // the schema cannot hold a full number
        entity.Property(x => x.VerifiedAtUtc).HasColumnType("datetimeoffset");
        entity.HasOne<Staff>().WithMany().HasForeignKey(x => x.StaffId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne<CounterTransaction>().WithMany().HasForeignKey(x => x.TransactionId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne<Branch>().WithMany().HasForeignKey(x => x.BranchCode).HasPrincipalKey(b => b.Code).OnDelete(DeleteBehavior.Restrict);
        entity.WithAuditColumns();
    }
}

public sealed class ShipmentScanConfiguration : IEntityTypeConfiguration<ShipmentScan>
{
    public void Configure(EntityTypeBuilder<ShipmentScan> entity)
    {
        entity.ToTable("ShipmentScans");
        entity.HasKey(x => x.Id);
        entity.Property(x => x.TrackingNumber).HasMaxLength(40).IsUnicode(false).IsRequired();
        entity.HasIndex(x => x.TrackingNumber);
        entity.Property(x => x.ScanType).HasConversion<string>().HasMaxLength(32).IsUnicode(false).IsRequired();
        entity.Property(x => x.CourierId).HasMaxLength(64).IsUnicode(false).IsRequired();
        entity.Property(x => x.Location).HasMaxLength(200).IsRequired();
        entity.Property(x => x.ScannedAtUtc).HasColumnType("datetimeoffset");
        entity.Property(x => x.Notes).HasMaxLength(1000);
        entity.WithAuditColumns();
    }
}

public sealed class AuditEntryConfiguration : IEntityTypeConfiguration<AuditEntryEntity>
{
    public void Configure(EntityTypeBuilder<AuditEntryEntity> entity)
    {
        entity.ToTable("AuditEntries");
        entity.HasKey(x => x.Id);
        entity.Property(x => x.Id).ValueGeneratedNever();
        entity.Property(x => x.Role).HasMaxLength(64).IsUnicode(false).IsRequired();
        entity.Property(x => x.BranchCode).HasMaxLength(32).IsUnicode(false).IsRequired();
        entity.Property(x => x.Controller).HasMaxLength(128).IsRequired();
        entity.Property(x => x.Action).HasMaxLength(128).IsRequired();
        entity.Property(x => x.CorrelationId).HasMaxLength(128).IsUnicode(false).IsRequired();
        entity.Property(x => x.ResultStatus).HasMaxLength(64).IsUnicode(false).IsRequired();
        entity.Property(x => x.RouteValues).HasMaxLength(512);
        entity.Property(x => x.DurationMs).HasColumnType("float");
        entity.Property(x => x.RecordedAtUtc).HasColumnType("datetimeoffset");
        entity.Property(x => x.Details).HasMaxLength(1024).IsRequired();
        entity.Property(x => x.RedactedFields).HasColumnType("nvarchar(max)");
        entity.Property(x => x.TransactionId).HasMaxLength(64);
        entity.Property(x => x.Reason).HasMaxLength(300);
        entity.Property(x => x.CreatedAtUtc).HasDefaultValueSql("SYSUTCDATETIME()");
        entity.Property(x => x.ModifiedAtUtc);
        // No foreign keys: audit rows are append-only and must outlive the rows they describe.
    }
}
