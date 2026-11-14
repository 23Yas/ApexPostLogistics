namespace ApexPost.Tests;

using System.Net.Http.Json;
using System.Text.Json;
using ApexPost.Infrastructure;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Hosts ApexPost.Api against a disposable LocalDB database (ApexPostTestApi_&lt;guid&gt;),
/// migrated on startup. The development database is never touched.
/// </summary>
// TEntryPoint only identifies the entry-point assembly; any public type from
// ApexPost.Api works and avoids ambiguity with ApexPost.Web's Program class.
public sealed class ApiWebApplicationFactory : WebApplicationFactory<ApexPost.Api.GlobalExceptionHandler>
{
    private readonly string _databaseName = $"ApexPostTestApi_{Guid.NewGuid():N}";

    public string TestConnectionString => new SqlConnectionStringBuilder
    {
        DataSource = @"(localdb)\MSSQLLocalDB",
        InitialCatalog = _databaseName,
        IntegratedSecurity = true,
        TrustServerCertificate = true,
        MultipleActiveResultSets = true
    }.ConnectionString;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.ConfigureAppConfiguration((_, configurationBuilder) =>
        {
            configurationBuilder.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:ApexPostDb"] = TestConnectionString,
                ["Jwt:Key"] = "test-signing-key-for-api-tests-only-0123456789abcdef",
                ["Jwt:Issuer"] = "apexpost-test",
                ["Jwt:Audience"] = "apexpost-courier-test",
                ["PartnerApiKeys:0"] = "test-partner-key-123"
            });
        });
    }

    /// <summary>Applies EF migrations to the fresh test database.</summary>
    public async Task EnsureDatabaseReadyAsync()
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApexPostDbContext>();
        await db.Database.MigrateAsync();
    }

    /// <summary>Issues a dev JWT (the /dev/token endpoint is Development-only).</summary>
    public async Task<string> GetTokenAsync(string role, string courierId = "courier-001")
    {
        var client = CreateClient();
        var response = await client.PostAsJsonAsync("/dev/token", new { courierId, role });
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<Dictionary<string, JsonElement>>();
        return body!["token"].GetString()!;
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
        {
            SqlConnection.ClearAllPools();
            try
            {
                using var context = new ApexPostDbContext(
                    new DbContextOptionsBuilder<ApexPostDbContext>().UseSqlServer(TestConnectionString).Options);
                context.Database.EnsureDeleted();
            }
            catch { /* best effort cleanup */ }
        }
    }
}
