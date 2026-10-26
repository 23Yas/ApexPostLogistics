namespace ApexPost.Tests;

using System.Net;
using ApexPost.Web;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

public sealed class WebIntegrationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public WebIntegrationTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task SecurityHeaders_ArePresent()
    {
        using var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        using var response = await client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(response.Headers.Contains("X-Content-Type-Options"));
        Assert.True(response.Headers.Contains("X-Frame-Options"));
        Assert.True(response.Headers.Contains("Referrer-Policy"));
        Assert.Contains("default-src 'self'", response.Headers.GetValues("Content-Security-Policy").Single());
    }

    [Fact]
    public async Task IntegrationEndpoint_RequiresApiKey()
    {
        using var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        using var response = await client.GetAsync("/integration/parcel-status");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task IntegrationEndpoint_RejectsBlockedIp()
    {
        CustomWebApplicationFactory.SetAllowedIps("10.0.0.1");
        using var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        client.DefaultRequestHeaders.Add("X-API-Key", "local-placeholder-api-key");

        using var response = await client.GetAsync("/integration/parcel-status");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task LoginEndpoint_ReturnsTooManyRequests_AfterLimitExceeded()
    {
        using var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var content = new FormUrlEncodedContent(new[] { new KeyValuePair<string, string>("email", "ava.reed@apexpost.local"), new KeyValuePair<string, string>("password", "ApexPostClerk123!") });

        for (var i = 0; i < 5; i++)
        {
            await client.PostAsync("/Account/Login", content);
        }

        using var response = await client.PostAsync("/Account/Login", content);

        Assert.Equal(HttpStatusCode.TooManyRequests, response.StatusCode);
    }

    [Fact]
    public async Task UnauthenticatedAccess_RedirectsToLogin()
    {
        using var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        using var response = await client.GetAsync("/Home/Dashboard");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Contains("/Account/Login?ReturnUrl=%2FHome%2FDashboard", response.Headers.Location?.OriginalString ?? string.Empty);
    }

    [Fact]
    public async Task Clerk_GetsForbidden_OnVoid()
    {
        using var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var loginResponse = await client.PostAsync("/Account/Login", new FormUrlEncodedContent(new[] { new KeyValuePair<string, string>("email", "ava.reed@apexpost.local"), new KeyValuePair<string, string>("password", "ApexPostClerk123!") }));
        Assert.Equal(HttpStatusCode.Redirect, loginResponse.StatusCode);

        using var response = await client.PostAsync("/Transactions/Void?transactionId=11111111-1111-1111-1111-111111111111&reason=Test", new FormUrlEncodedContent(new[] { new KeyValuePair<string, string>("reason", "Test") }));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task CorrelationHeader_IsReturned()
    {
        using var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        client.DefaultRequestHeaders.Add("X-Correlation-Id", "request-123");

        using var response = await client.GetAsync("/health");

        Assert.Equal("request-123", response.Headers.GetValues("X-Correlation-Id").Single());
    }
}

public sealed class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    private static string _allowedIps = "127.0.0.1,::1";

    public CustomWebApplicationFactory()
    {
    }

    public static void SetAllowedIps(string allowedIps)
    {
        _allowedIps = allowedIps;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.ConfigureAppConfiguration((_, configurationBuilder) =>
        {
            configurationBuilder.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Firewall:AllowedIps"] = _allowedIps,
                ["Integration:ApiKey"] = "local-placeholder-api-key",
                ["AppSettings:MaintenanceMode"] = "false"
            });
        });
    }
}
