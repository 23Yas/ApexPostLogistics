namespace ApexPost.Infrastructure;

using ApexPost.Core.Domain;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// Seeds reference data and history. Runs once (skipped when branches already exist).
/// Roles and ProductCategories are seeded by HasData in the migration, not here.
/// </summary>
public static class SeedDataBootstrap
{
    public static async Task InitializeAsync(ApexPostDbContext dbContext, CancellationToken cancellationToken = default)
    {
        if (await dbContext.Branches.AnyAsync(cancellationToken))
        {
            return;
        }

        var zones = BuildZones();
        var branchZone = SeedData.Branches
            .Select((branch, index) => (branch.Code, ZoneId: (index % zones.Count) + 1))
            .ToDictionary(x => x.Code, x => x.ZoneId);

        dbContext.Branches.AddRange(SeedData.Branches);
        dbContext.BranchCounters.AddRange(SeedData.Branches.SelectMany(branch =>
            branch.Counters.Select(counter => new BranchCounter { BranchCode = branch.Code, CounterCode = counter })));
        dbContext.Products.AddRange(SeedData.Products);
        dbContext.Staff.AddRange(SeedData.Staff);
        dbContext.StockItems.AddRange(SeedData.Stock);
        dbContext.ShippingZones.AddRange(zones);
        dbContext.ShippingRates.AddRange(BuildRates(zones));
        dbContext.CounterTransactions.AddRange(SeedData.Transactions);

        dbContext.Payments.AddRange(SeedData.Transactions.Select(tx => new Payment
        {
            CounterTransactionId = tx.Id,
            Method = tx.PaymentMethod,
            Amount = tx.Total.Amount,
            Status = tx.Status == TransactionStatus.Voided ? "Refunded" : "Captured",
            CreatedAtUtc = tx.CreatedAtUtc
        }));

        var shipments = new List<Shipment>();
        var reference = 0;
        foreach (var tx in SeedData.Transactions)
        {
            foreach (var line in tx.LineItems.Where(line => line.ServiceCode is not null))
            {
                reference++;
                shipments.Add(new Shipment
                {
                    CounterTransactionId = tx.Id,
                    BranchCode = tx.BranchCode,
                    ZoneId = branchZone[tx.BranchCode],
                    ServiceLevel = ParseServiceLevel(line.ServiceCode!),
                    WeightKg = 1.00m,
                    Price = line.UnitPrice.Amount * line.Quantity,
                    TrackingReference = $"APX{reference:D8}",
                    Status = tx.Status == TransactionStatus.Voided ? "Returned" : "Delivered",
                    CreatedAtUtc = tx.CreatedAtUtc
                });
            }
        }
        dbContext.Shipments.AddRange(shipments);

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static List<ShippingZone> BuildZones()
    {
        var regions = new[] { "Midlands", "North", "North", "South", "South", "South West", "Wales", "Scotland", "East", "West" };
        return Enumerable.Range(1, 10)
            .Select(i => new ShippingZone
            {
                Id = i,
                Code = $"Z{i:00}",
                Name = $"Zone {i:00}",
                Region = regions[i - 1]
            })
            .ToList();
    }

    private static List<ShippingRate> BuildRates(IEnumerable<ShippingZone> zones)
    {
        var rates = new List<ShippingRate>();
        var id = 0;
        foreach (var zone in zones)
        {
            foreach (var level in Enum.GetValues<ServiceLevel>())
            {
                id++;
                var tier = (int)level;
                rates.Add(new ShippingRate
                {
                    Id = id,
                    ZoneId = zone.Id,
                    ServiceLevel = level,
                    MaxWeightKg = 10m,
                    BaseRate = 4.50m + (zone.Id * 0.50m) + (tier * 2.00m),
                    PerKgRate = 0.80m + (tier * 0.60m)
                });
            }
        }
        return rates;
    }

    private static ServiceLevel ParseServiceLevel(string serviceCode)
        => serviceCode.Contains("EXPRESS", StringComparison.OrdinalIgnoreCase) ? ServiceLevel.Express
         : serviceCode.Contains("REGISTERED", StringComparison.OrdinalIgnoreCase) ? ServiceLevel.Registered
         : ServiceLevel.Standard;
}
