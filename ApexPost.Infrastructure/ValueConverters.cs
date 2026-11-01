namespace ApexPost.Infrastructure;

using ApexPost.Core.Domain;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

public static class ApexPostValueConverters
{
    public static ValueConverter<BranchCode, string> BranchCodeConverter { get; } =
        new(v => v.Value, v => new BranchCode(v));

    public static ValueConverter<Sku, string> SkuConverter { get; } =
        new(v => v.Value, v => new Sku(v));

    public static ValueConverter<Money, decimal> MoneyConverter { get; } =
        new(v => v.Amount, v => new Money(v));

    public static ValueConverter<Weight, decimal> WeightConverter { get; } =
        new(v => v.ValueKg, v => new Weight(v));
}

public sealed class BranchCodeValueConverter : ValueConverter<BranchCode, string>
{
    public BranchCodeValueConverter()
        : base(v => v.Value, v => new BranchCode(v))
    {
    }
}

public sealed class SkuValueConverter : ValueConverter<Sku, string>
{
    public SkuValueConverter()
        : base(v => v.Value, v => new Sku(v))
    {
    }
}

public sealed class MoneyValueConverter : ValueConverter<Money, decimal>
{
    public MoneyValueConverter()
        : base(v => v.Amount, v => new Money(v))
    {
    }
}

public sealed class WeightValueConverter : ValueConverter<Weight, decimal>
{
    public WeightValueConverter()
        : base(v => v.ValueKg, v => new Weight(v))
    {
    }
}
