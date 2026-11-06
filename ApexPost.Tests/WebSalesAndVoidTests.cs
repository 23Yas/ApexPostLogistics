namespace ApexPost.Tests;

using ApexPost.Core.Domain;
using ApexPost.Infrastructure;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Net;
using System.Text.RegularExpressions;
[Collection("WebTests")]
public sealed class WebSalesAndVoidTests : IClassFixture<CustomWebApplicationFactory>
{
    private const string ClerkEmail = "ava.reed@apexpost.local";
    private const string ClerkPassword = "ApexPostClerk123!";
    private const string ManagerEmail = "chloe.moss@apexpost.local";
    private const string ManagerPassword = "ApexPostManager123!";
    private const string SkuCode = "SKU-1001";

    private static readonly BranchCode Branch = new("BR-001");

    private readonly CustomWebApplicationFactory _factory;

    public WebSalesAndVoidTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _factory.EnsureDatabaseReady();
        CustomWebApplicationFactory.ResetRateLimits();
    }

    [Fact]
    public async Task Sale_posted_through_the_web_is_saved_and_decrements_stock()
    {
        using var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        await LoginAsync(client, ClerkEmail, ClerkPassword);
        var token = await GetAntiForgeryTokenAsync(client);

        int stockBefore;
        int salesBefore;
        await using (var reader = _factory.CreateDbContext())
        {
            stockBefore = (await reader.StockItems.SingleAsync(s => s.BranchCode == Branch && s.ProductSku == new Sku(SkuCode))).QuantityOnHand;
            salesBefore = await reader.CounterTransactions.CountAsync(t => t.BranchCode == Branch);
        }

        using var response = await client.PostAsync("/Transactions/Create", new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("__RequestVerificationToken", token),
            new KeyValuePair<string, string>("productSku", SkuCode),
            new KeyValuePair<string, string>("quantity", "1"),
            new KeyValuePair<string, string>("paymentMethod", "Cash"),
            new KeyValuePair<string, string>("serviceLevel", "None"),
            new KeyValuePair<string, string>("counterId", "C1")
        }));

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);

        await using var check = _factory.CreateDbContext();
        var stockAfter = (await check.StockItems.SingleAsync(s => s.BranchCode == Branch && s.ProductSku == new Sku(SkuCode))).QuantityOnHand;
        var salesAfter = await check.CounterTransactions.CountAsync(t => t.BranchCode == Branch);

        Assert.Equal(stockBefore - 1, stockAfter);
        Assert.Equal(salesBefore + 1, salesAfter);
    }

    [Fact]
    public async Task Branch_manager_can_void_a_paid_sale_from_their_own_branch()
    {
        Guid transactionId;
        await using (var reader = _factory.CreateDbContext())
        {
            transactionId = await reader.CounterTransactions
                .Where(t => t.BranchCode == Branch && t.Status == TransactionStatus.Paid)
                .Select(t => t.Id)
                .FirstAsync();
        }

        using var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        await LoginAsync(client, ManagerEmail, ManagerPassword);
        var token = await GetAntiForgeryTokenAsync(client);

        using var response = await client.PostAsync("/Transactions/Void", new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("__RequestVerificationToken", token),
            new KeyValuePair<string, string>("transactionId", transactionId.ToString()),
            new KeyValuePair<string, string>("reason", "Duplicate entry")
        }));

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);

        await using var check = _factory.CreateDbContext();
        var status = (await check.CounterTransactions.SingleAsync(t => t.Id == transactionId)).Status;
        Assert.Equal(TransactionStatus.Voided, status);
    }

    [Fact]
    public async Task Daily_report_total_matches_the_sum_of_the_branch_transactions()
    {
        decimal expected;
        await using (var reader = _factory.CreateDbContext())
        {
            var totals = await reader.CounterTransactions
                .AsNoTracking()
                .Where(t => t.BranchCode == Branch)
                .Select(t => t.Total)
                .ToListAsync();
            expected = totals.Sum(total => total.Amount);
        }

        Assert.True(expected > 0m, "The seeded branch should have sales to report.");

        using var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        await LoginAsync(client, ClerkEmail, ClerkPassword);

        using var page = await client.GetAsync("/Reports/Daily");
        var html = await page.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, page.StatusCode);
        Assert.Contains($"\u00A3{expected.ToString("N2", CultureInfo.InvariantCulture)}", html);
    }

    private static async Task LoginAsync(HttpClient client, string email, string password)
    {
        using var response = await client.PostAsync("/Account/Login", new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("email", email),
            new KeyValuePair<string, string>("password", password)
        }));

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
    }

    private static async Task<string> GetAntiForgeryTokenAsync(HttpClient client)
    {
        using var page = await client.GetAsync("/Transactions/New");
        var html = await page.Content.ReadAsStringAsync();

        var match = Regex.Match(html, "name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"");
        Assert.True(match.Success, "The anti-forgery token was not found on the page.");
        return match.Groups[1].Value;
    }
}