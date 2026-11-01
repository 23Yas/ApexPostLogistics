namespace ApexPost.Infrastructure;

using System.Text.Json;
using ApexPost.Core.Application.Interfaces;
using ApexPost.Core.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

public sealed class ApexPostDbContext : DbContext
{
    public ApexPostDbContext(DbContextOptions<ApexPostDbContext> options)
        : base(options)
    {
    }

    public DbSet<Branch> Branches => Set<Branch>();
    public DbSet<Staff> Staff => Set<Staff>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<StockItem> StockItems => Set<StockItem>();
    public DbSet<CounterTransaction> CounterTransactions => Set<CounterTransaction>();
    public DbSet<MoneyOrder> MoneyOrders => Set<MoneyOrder>();
    public DbSet<IdentityVerification> IdentityVerifications => Set<IdentityVerification>();
    public DbSet<AuditEntryEntity> AuditEntries => Set<AuditEntryEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Branch>(entity =>
        {
            entity.ToTable("Branches");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).ValueGeneratedOnAdd();
            entity.Property(x => x.Code).HasConversion(v => v.Value, v => new BranchCode(v)).HasMaxLength(32).IsRequired();
            entity.HasIndex(x => x.Code).IsUnique();
            entity.Property(x => x.Name).HasMaxLength(200).IsRequired();
            entity.Property(x => x.Region).HasMaxLength(100).IsRequired();
            entity.Property(x => x.OpeningHours).HasMaxLength(100).IsRequired();
            entity.Property(x => x.IsActive).HasDefaultValue(true);
            entity.Property(x => x.Counters)
                .HasConversion(
                    v => JsonSerializer.Serialize(v ?? new List<string>(), JsonSerializerOptions.Default),
                    v => JsonSerializer.Deserialize<List<string>>(v ?? "[]", JsonSerializerOptions.Default) ?? new List<string>())
                .HasColumnType("nvarchar(max)");
            entity.Property(x => x.CreatedAtUtc).HasDefaultValueSql("SYSUTCDATETIME()");
            entity.Property<string>("CreatedBy").HasDefaultValue("system").HasMaxLength(128);
            entity.Property(x => x.ModifiedAtUtc);
            entity.Property<string>("ModifiedBy").HasMaxLength(128);
        });

        modelBuilder.Entity<Staff>(entity =>
        {
            entity.ToTable("Staff");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).ValueGeneratedOnAdd();
            entity.Property(x => x.Name).HasMaxLength(200).IsRequired();
            entity.Property(x => x.Email).HasMaxLength(200).IsRequired();
            entity.HasIndex(x => x.Email).IsUnique();
            entity.Property(x => x.PasswordHash).HasMaxLength(512).IsRequired();
            entity.Property(x => x.Role).HasConversion<string>().HasMaxLength(32).IsRequired();
            entity.Property(x => x.HomeBranch).HasConversion(v => v.Value, v => new BranchCode(v)).HasMaxLength(32).IsRequired();
            entity.Property(x => x.IsActive).HasDefaultValue(true);
            entity.Property(x => x.CreatedAtUtc).HasDefaultValueSql("SYSUTCDATETIME()");
            entity.Property<string>("CreatedBy").HasDefaultValue("system").HasMaxLength(128);
            entity.Property(x => x.ModifiedAtUtc);
            entity.Property<string>("ModifiedBy").HasMaxLength(128);
        });

        modelBuilder.Entity<Product>(entity =>
        {
            entity.ToTable("Products");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).ValueGeneratedOnAdd();
            entity.Property(x => x.Sku).HasConversion(v => v.Value, v => new Sku(v)).HasMaxLength(64).IsRequired();
            entity.HasIndex(x => x.Sku).IsUnique();
            entity.Property(x => x.Name).HasMaxLength(200).IsRequired();
            entity.Property(x => x.Category).HasConversion<string>().HasMaxLength(32).IsRequired();
            entity.Property(x => x.UnitPrice).HasConversion(v => v.Amount, v => new Money(v)).HasColumnType("decimal(18,2)");
            entity.Property(x => x.TaxCategory).HasConversion<string>().HasMaxLength(32).IsRequired();
            entity.Property(x => x.CreatedAtUtc).HasDefaultValueSql("SYSUTCDATETIME()");
            entity.Property<string>("CreatedBy").HasDefaultValue("system").HasMaxLength(128);
            entity.Property(x => x.ModifiedAtUtc);
            entity.Property<string>("ModifiedBy").HasMaxLength(128);
        });

        modelBuilder.Entity<StockItem>(entity =>
        {
            entity.ToTable("BranchStock", table =>
            {
                table.HasCheckConstraint("CK_BranchStock_QuantityOnHand_NonNegative", "QuantityOnHand >= 0");
                table.HasCheckConstraint("CK_BranchStock_ReorderThreshold_NonNegative", "ReorderThreshold >= 0");
            });
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).ValueGeneratedOnAdd();
            entity.Property(x => x.BranchCode).HasConversion(v => v.Value, v => new BranchCode(v)).HasMaxLength(32).IsRequired();
            entity.Property(x => x.ProductSku).HasConversion(v => v.Value, v => new Sku(v)).HasMaxLength(64).IsRequired();
            entity.HasIndex(x => new { x.BranchCode, x.ProductSku }).IsUnique();
            entity.HasIndex(x => new { x.BranchCode, x.QuantityOnHand }).HasDatabaseName("IX_BranchStock_BranchCode_QuantityOnHand");
            entity.Property(x => x.QuantityOnHand).HasColumnType("int");
            entity.Property(x => x.ReorderThreshold).HasDefaultValue(5);
            entity.Property<byte[]>("RowVersion").IsRowVersion();
            entity.Property(x => x.CreatedAtUtc).HasDefaultValueSql("SYSUTCDATETIME()");
            entity.Property<string>("CreatedBy").HasDefaultValue("system").HasMaxLength(128);
            entity.Property(x => x.ModifiedAtUtc);
            entity.Property<string>("ModifiedBy").HasMaxLength(128);
        });

        modelBuilder.Entity<IdentityVerification>(entity =>
        {
            entity.ToTable("IdentityVerifications");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).ValueGeneratedOnAdd();
            entity.Property(x => x.StaffId).IsRequired();
            entity.Property(x => x.TransactionId).IsRequired();
            entity.Property(x => x.BranchCode).HasConversion(v => v.Value, v => new BranchCode(v)).HasMaxLength(32).IsRequired();
            entity.Property(x => x.DocumentType).HasMaxLength(64).IsRequired();
            entity.Property(x => x.IsVerified).HasDefaultValue(false);
            entity.Property(x => x.MaskedIdentifier).HasMaxLength(20).IsRequired();
            entity.Property(x => x.VerifiedAtUtc).HasColumnType("datetimeoffset");
            entity.Property(x => x.CreatedAtUtc).HasDefaultValueSql("SYSUTCDATETIME()");
            entity.Property<string>("CreatedBy").HasDefaultValue("system").HasMaxLength(128);
            entity.Property(x => x.ModifiedAtUtc);
            entity.Property<string>("ModifiedBy").HasMaxLength(128);
        });

        modelBuilder.Entity<MoneyOrder>(entity =>
        {
            entity.ToTable("MoneyOrders", table =>
            {
                table.HasCheckConstraint("CK_MoneyOrders_Amount_Positive", "Amount > 0");
                table.HasCheckConstraint("CK_MoneyOrders_Fee_NonNegative", "Fee >= 0");
            });
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).ValueGeneratedOnAdd();
            entity.Property(x => x.TransactionId).IsRequired();
            entity.Property(x => x.BranchCode).HasConversion(v => v.Value, v => new BranchCode(v)).HasMaxLength(32).IsRequired();
            entity.HasIndex(x => new { x.BranchCode, x.CreatedAtUtc }).HasDatabaseName("IX_MoneyOrders_BranchCode_CreatedAtUtc");
            entity.Property(x => x.Amount).HasConversion(v => v.Amount, v => new Money(v)).HasColumnType("decimal(18,2)");
            entity.Property(x => x.Sender).HasMaxLength(200).IsRequired();
            entity.Property(x => x.Recipient).HasMaxLength(200).IsRequired();
            entity.Property(x => x.Fee).HasConversion(v => v.Amount, v => new Money(v)).HasColumnType("decimal(18,2)");
            entity.Property(x => x.RequiresIdentityVerification).HasDefaultValue(false);
            entity.Property(x => x.IdentityVerificationComplete).HasDefaultValue(false);
            entity.Property(x => x.Status).HasMaxLength(32).HasDefaultValue("Pending");
            entity.Property(x => x.CreatedAtUtc).HasDefaultValueSql("SYSUTCDATETIME()");
            entity.Property<string>("CreatedBy").HasDefaultValue("system").HasMaxLength(128);
            entity.Property(x => x.ModifiedAtUtc);
            entity.Property<string>("ModifiedBy").HasMaxLength(128);
        });

        modelBuilder.Entity<CounterTransaction>(entity =>
        {
            entity.ToTable("CounterTransactions", table =>
            {
                table.HasCheckConstraint("CK_CounterTransactions_Total_Positive", "Total >= 0");
                table.HasCheckConstraint("CK_CounterTransactions_Status_Valid", "Status IN ('Open','Paid','Voided','Refunded')");
            });
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).ValueGeneratedOnAdd();
            entity.Property(x => x.BranchCode).HasConversion(v => v.Value, v => new BranchCode(v)).HasMaxLength(32).IsRequired();
            entity.HasIndex(x => new { x.BranchCode, x.CreatedAtUtc }).HasDatabaseName("IX_CounterTransactions_BranchCode_CreatedAtUtc");
            entity.Property(x => x.CounterId).HasMaxLength(32).IsRequired();
            entity.Property(x => x.ClerkId).IsRequired();
            entity.Property(x => x.PaymentMethod).HasConversion<string>().HasMaxLength(32).IsRequired();
            entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(32).IsRequired();
            entity.Property(x => x.SubTotal).HasConversion(v => v.Amount, v => new Money(v)).HasColumnType("decimal(18,2)");
            entity.Property(x => x.TaxTotal).HasConversion(v => v.Amount, v => new Money(v)).HasColumnType("decimal(18,2)");
            entity.Property(x => x.Total).HasConversion(v => v.Amount, v => new Money(v)).HasColumnType("decimal(18,2)");
            entity.Property(x => x.ClosedAtUtc).HasColumnType("datetimeoffset");
            entity.Property(x => x.VoidReason).HasMaxLength(200);
            entity.Property(x => x.AuditCorrelationId).HasMaxLength(128);
            entity.Property<byte[]>("RowVersion").IsRowVersion();
            entity.Property(x => x.CreatedAtUtc).HasDefaultValueSql("SYSUTCDATETIME()");
            entity.Property<string>("CreatedBy").HasDefaultValue("system").HasMaxLength(128);
            entity.Property(x => x.ModifiedAtUtc);
            entity.Property<string>("ModifiedBy").HasMaxLength(128);
        });

        modelBuilder.Entity<CounterTransaction>()
            .OwnsMany(transaction => transaction.LineItems, line =>
            {
                line.WithOwner().HasForeignKey("CounterTransactionId");
                line.ToTable("TransactionLines", table =>
                {
                    table.HasCheckConstraint("CK_TransactionLines_Quantity_Positive", "Quantity > 0");
                });
                line.HasKey(x => x.Id);
                line.Property(x => x.Id).ValueGeneratedOnAdd();
                line.Property(x => x.Description).HasMaxLength(200).IsRequired();
                line.HasIndex(x => x.ServiceCode).HasDatabaseName("IX_TransactionLines_ServiceCode");
                line.Property(x => x.ProductSku)
                    .HasConversion(v => v.HasValue ? v.Value.Value : null, v => string.IsNullOrWhiteSpace(v) ? null : new Sku(v))
                    .HasMaxLength(64);
                line.Property(x => x.ServiceCode).HasMaxLength(64);
                line.Property(x => x.Quantity).IsRequired();
                line.Property(x => x.UnitPrice).HasConversion(v => v.Amount, v => new Money(v)).HasColumnType("decimal(18,2)");
                line.Property(x => x.TaxCategory).HasConversion<string>().HasMaxLength(32).IsRequired();
                line.Property(x => x.CreatedAtUtc).HasDefaultValueSql("SYSUTCDATETIME()");
                line.Property<string>("CreatedBy").HasDefaultValue("system").HasMaxLength(128);
                line.Property(x => x.ModifiedAtUtc);
                line.Property<string>("ModifiedBy").HasMaxLength(128);
            });

        modelBuilder.Entity<AuditEntryEntity>(entity =>
        {
            entity.ToTable("AuditEntries");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).ValueGeneratedOnAdd();
            entity.Property(x => x.StaffId).IsRequired();
            entity.Property(x => x.Role).HasMaxLength(64).IsRequired();
            entity.Property(x => x.BranchCode).HasMaxLength(32).IsRequired();
            entity.Property(x => x.Controller).HasMaxLength(128).IsRequired();
            entity.Property(x => x.Action).HasMaxLength(128).IsRequired();
            entity.Property(x => x.CorrelationId).HasMaxLength(128).IsRequired();
            entity.Property(x => x.ResultStatus).HasMaxLength(64).IsRequired();
            entity.Property(x => x.RouteValues).HasMaxLength(512);
            entity.Property(x => x.DurationMs).HasColumnType("float");
            entity.Property(x => x.RecordedAtUtc).HasColumnType("datetimeoffset");
            entity.Property(x => x.Details).HasMaxLength(1024).IsRequired();
            entity.Property(x => x.RedactedFields).HasColumnType("nvarchar(max)");
            entity.Property(x => x.TransactionId).HasMaxLength(64);
            entity.Property(x => x.Reason).HasMaxLength(300);
            entity.Property(x => x.CreatedAtUtc).HasDefaultValueSql("SYSUTCDATETIME()");
            entity.Property(x => x.ModifiedAtUtc);
        });
    }
}

public sealed class AuditEntryEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid StaffId { get; set; }
    public string Role { get; set; } = string.Empty;
    public string BranchCode { get; set; } = string.Empty;
    public string Controller { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string CorrelationId { get; set; } = string.Empty;
    public string ResultStatus { get; set; } = string.Empty;
    public string RouteValues { get; set; } = string.Empty;
    public double DurationMs { get; set; }
    public DateTimeOffset RecordedAtUtc { get; set; }
    public string Details { get; set; } = string.Empty;
    public string RedactedFields { get; set; } = string.Empty;
    public string? TransactionId { get; set; }
    public string? Reason { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? ModifiedAtUtc { get; set; }
}
