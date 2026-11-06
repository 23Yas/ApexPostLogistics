namespace ApexPost.Tests;

using ApexPost.Infrastructure;
using ApexPost.Web;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

/// <summary>
/// Hosts the app against a disposable database named ApexPostTest_&lt;guid&gt;. The development database
/// (ApexPostDb) is never touched. The database is dropped when the factory is disposed.
/// </summary>
public sealed class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    private static string _allowedIps = "127.0.0.1,::1";
    private readonly string _databaseName = $"ApexPostTest_{Guid.NewGuid():N}";

    public static void SetAllowedIps(string allowedIps)
    {
        _allowedIps = allowedIps;
    }

    /// <summary>
    /// The login rate limiter is a static counter shared by every test host in the process.
    /// Clearing it gives each web test a fresh budget. Test-only: production code is unchanged.
    /// </summary>
    public static void ResetRateLimits()
    {
        var field = typeof(ApexPost.Web.Middleware.RateLimitMiddleware).GetField(
            "RequestCounts",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

        if (field?.GetValue(null) is System.Collections.IDictionary counts)
        {
            counts.Clear();
        }
    }

    public string TestConnectionString => new SqlConnectionStringBuilder
    {
        DataSource = @"(localdb)\MSSQLLocalDB",
        InitialCatalog = _databaseName,
        IntegratedSecurity = true,
        TrustServerCertificate = true,
        MultipleActiveResultSets = true
    }.ConnectionString;

    public ApexPostDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ApexPostDbContext>()
            .UseSqlServer(TestConnectionString)
            .Options;
        return new ApexPostDbContext(options);
    }

    /// <summary>Starts the host, which runs Migrate() and the seed against the test database.</summary>
    public void EnsureDatabaseReady()
    {
        _ = Services;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.ConfigureAppConfiguration((_, configurationBuilder) =>
        {
            configurationBuilder.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:ApexPostDb"] = TestConnectionString,
                ["Firewall:AllowedIps"] = _allowedIps,
                ["Integration:ApiKey"] = "local-placeholder-api-key",
                ["AppSettings:MaintenanceMode"] = "false"
            });
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
        {
            SqlConnection.ClearAllPools();
            using var context = CreateDbContext();
            context.Database.EnsureDeleted();
        }
    }
}