namespace ApexPost.Tests;

using ApexPost.Core.Domain;
using ApexPost.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Xunit.Abstractions;

public sealed class QuerySqlSamplesTests
{
    private readonly ITestOutputHelper _output;

    public QuerySqlSamplesTests(ITestOutputHelper output)
    {
        _output = output;
    }

    private static ApexPostDbContext NewContext() => new(new DbContextOptionsBuilder<ApexPostDbContext>()
        .UseSqlServer("Server=(localdb)\\MSSQLLocalDB;Database=NotUsed;Trusted_Connection=True;TrustServerCertificate=True;")
        .Options);

    [Fact]
    public void DailySales_query_sql_is_printed_for_performance_md()
    {
        using var db = NewContext();
        var from = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var to = new DateTimeOffset(2026, 1, 2, 0, 0, 0, TimeSpan.Zero);

        var sql = db.CounterTransactions
            .Where(t => t.BranchCode == new BranchCode("BR-001") && t.CreatedAtUtc >= from && t.CreatedAtUtc < to)
            .ToQueryString();

        _output.WriteLine(sql);
        Assert.Contains("FROM [CounterTransactions]", sql);
    }

    [Fact]
    public void StockLookup_query_sql_is_printed_for_performance_md()
    {
        using var db = NewContext();

        var sql = db.StockItems
            .Where(s => s.BranchCode == new BranchCode("BR-001") && s.ProductSku == new Sku("SKU-1001"))
            .ToQueryString();

        _output.WriteLine(sql);
        Assert.Contains("FROM [BranchStock]", sql);
    }

    [Fact]
    public void LowStock_query_sql_is_printed_for_performance_md()
    {
        using var db = NewContext();

        var sql = db.StockItems
            .Where(s => s.BranchCode == new BranchCode("BR-001") && s.QuantityOnHand <= 5)
            .ToQueryString();

        _output.WriteLine(sql);
        Assert.Contains("FROM [BranchStock]", sql);
    }
}