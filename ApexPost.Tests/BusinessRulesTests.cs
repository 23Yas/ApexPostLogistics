namespace ApexPost.Tests;

using ApexPost.Core.Application.Services;
using ApexPost.Core.Domain;

public sealed class BusinessRulesTests
{
    [Fact]
    public void StockRule_AllowsWhenStockSufficient()
    {
        var stock = new StockItem { BranchCode = new BranchCode("BR-001"), ProductSku = new Sku("SKU-1001"), QuantityOnHand = 10 };

        Assert.True(BusinessRules.CanFulfillOrder(stock, 7));
    }

    [Fact]
    public void StockRule_RejectsWhenStockInsufficient()
    {
        var stock = new StockItem { BranchCode = new BranchCode("BR-001"), ProductSku = new Sku("SKU-1001"), QuantityOnHand = 3 };

        Assert.False(BusinessRules.CanFulfillOrder(stock, 4));
    }

    [Fact]
    public void MoneyOrderRequiresIdentityVerification_WhenThresholdExceeded()
    {
        var moneyOrder = new MoneyOrder { Amount = new Money(2000m), Sender = "A", Recipient = "B" };

        Assert.True(BusinessRules.RequiresIdentityVerification(moneyOrder));
    }

    [Fact]
    public void MoneyOrderIdentityVerification_IsSatisfied_WhenVerified()
    {
        var moneyOrder = new MoneyOrder { Amount = new Money(2500m), Sender = "A", Recipient = "B" };
        var verification = new IdentityVerification { IsVerified = true, DocumentType = "Passport" };

        Assert.True(BusinessRules.IsIdentityVerificationSatisfied(moneyOrder, verification));
    }

    [Fact]
    public void ParcelWeight_IsRejected_WhenAboveLimit()
    {
        var quote = new ShippingQuote { Zone = "North", ServiceLevel = ServiceLevel.Registered, Weight = new Weight(12m), IsLetter = false };

        Assert.False(BusinessRules.IsParcelWithinWeightLimit(quote, 10m));
    }

    [Fact]
    public async Task PricingService_CalculatesShippingRateForZoneAsync()
    {
        var provider = new FixedRateProvider(1.75m);
        var service = new PricingService(provider);
        var quote = new ShippingQuote { Zone = "North", ServiceLevel = ServiceLevel.Express, Weight = new Weight(5m), IsLetter = false };

        var result = await service.CalculateShippingAsync(quote);

        Assert.Equal(8.75m, result.Amount);
    }

    private sealed class FixedRateProvider : IShippingRateProvider
    {
        private readonly decimal _rate;

        public FixedRateProvider(decimal rate)
        {
            _rate = rate;
        }

        public Task<decimal> GetRateAsync(string zone, ServiceLevel serviceLevel, bool isLetter, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_rate);
        }
    }
}
