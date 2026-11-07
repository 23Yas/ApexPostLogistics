namespace ApexPost.Infrastructure;

using ApexPost.Core.Domain;
using ApexPost.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// Mapping lives in ApexPost.Infrastructure/Configurations (one IEntityTypeConfiguration per entity).
/// The AuditEntryEntity class that used to be declared at the bottom of this file was a duplicate of the
/// one in ApexPost.Core.Domain; the Domain one is used now.
/// </summary>
public sealed class ApexPostDbContext : DbContext
{
    public ApexPostDbContext(DbContextOptions<ApexPostDbContext> options)
        : base(options)
    {
    }

    public DbSet<RoleRecord> Roles => Set<RoleRecord>();
    public DbSet<ProductCategoryRecord> ProductCategories => Set<ProductCategoryRecord>();
    public DbSet<Branch> Branches => Set<Branch>();
    public DbSet<BranchCounter> BranchCounters => Set<BranchCounter>();
    public DbSet<Staff> Staff => Set<Staff>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<StockItem> StockItems => Set<StockItem>();
    public DbSet<StockAdjustment> StockAdjustments => Set<StockAdjustment>();
    public DbSet<ShippingZone> ShippingZones => Set<ShippingZone>();
    public DbSet<ShippingRate> ShippingRates => Set<ShippingRate>();
    public DbSet<Shipment> Shipments => Set<Shipment>();
    public DbSet<ShipmentScan> ShipmentScans => Set<ShipmentScan>();
    public DbSet<CounterTransaction> CounterTransactions => Set<CounterTransaction>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<MoneyOrder> MoneyOrders => Set<MoneyOrder>();
    public DbSet<IdentityVerificationRecord> IdentityVerifications => Set<IdentityVerificationRecord>();
    public DbSet<AuditEntryEntity> AuditEntries => Set<AuditEntryEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApexPostDbContext).Assembly);
    }
}
