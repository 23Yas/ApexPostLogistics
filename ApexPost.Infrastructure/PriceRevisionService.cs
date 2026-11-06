namespace ApexPost.Infrastructure;

using ApexPost.Core.Application.Interfaces;
using ApexPost.Core.Domain;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// Sets the unit price of every product in a category with one UPDATE statement.
/// ExecuteUpdateAsync runs immediately, outside the change tracker, so no SaveChangesAsync is involved.
/// </summary>
public sealed class EfPriceRevisionService : IPriceRevisionService
{
    private readonly ApexPostDbContext _dbContext;

    public EfPriceRevisionService(ApexPostDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<int> SetCategoryPriceAsync(ProductCategory category, Money newPrice, CancellationToken cancellationToken = default)
    {
        if (newPrice.Amount < 0m)
        {
            throw new DomainException("A product price cannot be negative.");
        }

        return await _dbContext.Products
            .Where(product => product.Category == category)
            .ExecuteUpdateAsync(setters => setters
                    .SetProperty(product => product.UnitPrice, newPrice)
                    .SetProperty(product => product.ModifiedAtUtc, DateTimeOffset.UtcNow),
                cancellationToken);
    }
}