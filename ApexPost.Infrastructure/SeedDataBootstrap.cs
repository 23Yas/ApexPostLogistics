namespace ApexPost.Infrastructure;

using ApexPost.Core.Domain;
using Microsoft.EntityFrameworkCore;

public static class SeedDataBootstrap
{
    public static async Task InitializeAsync(ApexPostDbContext dbContext, CancellationToken cancellationToken = default)
    {
        if (await dbContext.Branches.AnyAsync(cancellationToken))
        {
            return;
        }

        foreach (var branch in SeedData.Branches)
        {
            dbContext.Branches.Add(branch);
        }

        foreach (var product in SeedData.Products)
        {
            dbContext.Products.Add(product);
        }

        foreach (var staff in SeedData.Staff)
        {
            dbContext.Staff.Add(staff);
        }

        foreach (var stockItem in SeedData.Stock)
        {
            dbContext.StockItems.Add(stockItem);
        }

        foreach (var transaction in SeedData.Transactions)
        {
            dbContext.CounterTransactions.Add(transaction);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
