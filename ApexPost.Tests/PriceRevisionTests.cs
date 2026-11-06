namespace ApexPost.Tests;

using ApexPost.Core.Domain;
using ApexPost.Infrastructure;
using Microsoft.EntityFrameworkCore;

public sealed class PriceRevisionTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public PriceRevisionTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _factory.EnsureDatabaseReady();
    }

    [Fact]
    public async Task Category_price_revision_updates_every_product_in_that_category()
    {
        int changed;
        await using (var writer = _factory.CreateDbContext())
        {
            var service = new EfPriceRevisionService(writer);
            changed = await service.SetCategoryPriceAsync(ProductCategory.Stationery, new Money(4.25m));
        }

        await using var check = _factory.CreateDbContext();
        var products = await check.Products
            .AsNoTracking()
            .Where(product => product.Category == ProductCategory.Stationery)
            .ToListAsync();

        Assert.True(changed > 0);
        Assert.Equal(changed, products.Count);
        Assert.All(products, product => Assert.Equal(4.25m, product.UnitPrice.Amount));
    }

    [Fact]
    public async Task Negative_price_is_rejected_before_any_update()
    {
        await using var writer = _factory.CreateDbContext();
        var service = new EfPriceRevisionService(writer);

        await Assert.ThrowsAsync<DomainException>(() =>
            service.SetCategoryPriceAsync(ProductCategory.Stationery, new Money(-1m)));
    }
}