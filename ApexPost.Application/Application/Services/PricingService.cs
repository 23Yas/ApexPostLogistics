namespace ApexPost.Core.Application.Services;

using ApexPost.Core.Domain;

public interface IShippingRateProvider
{
    Task<decimal> GetRateAsync(string zone, ServiceLevel serviceLevel, bool isLetter, CancellationToken cancellationToken = default);
}

public sealed class PricingService
{
    private readonly IShippingRateProvider _shippingRateProvider;

    public PricingService(IShippingRateProvider shippingRateProvider)
    {
        _shippingRateProvider = shippingRateProvider;
    }

    public Money CalculateLineTotal(TransactionLine line, int quantity)
    {
        var unitTotal = line.UnitPrice * quantity;
        return unitTotal;
    }

    public async Task<Money> CalculateShippingAsync(ShippingQuote quote, CancellationToken cancellationToken = default)
    {
        var rate = await _shippingRateProvider.GetRateAsync(quote.Zone, quote.ServiceLevel, quote.IsLetter, cancellationToken);
        var weightCharge = quote.Weight.ValueKg * rate;
        return new Money(weightCharge);
    }

    public static bool IsParcelWithinWeightLimit(ShippingQuote quote)
    {
        var maxWeight = quote.ServiceLevel switch
        {
            ServiceLevel.Standard => 20m,
            ServiceLevel.Express => 15m,
            ServiceLevel.Registered => 10m,
            _ => 0m
        };

        return quote.Weight.ValueKg <= maxWeight;
    }

    public static Money CalculateTaxFor(Money subtotal, TaxCategory taxCategory)
    {
        var rate = taxCategory switch
        {
            TaxCategory.Standard => 0.15m,
            TaxCategory.Reduced => 0.05m,
            TaxCategory.Exempt => 0m,
            _ => 0m
        };

        return new Money(subtotal.Amount * rate);
    }
}
