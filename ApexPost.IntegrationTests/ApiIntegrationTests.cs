namespace ApexPost.Tests;

using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

/// <summary>Integration tests for the versioned ApexPost.Api (JWT, roles, partner key, errors).</summary>
public sealed class ApiIntegrationTests : IClassFixture<ApiWebApplicationFactory>
{
    private readonly ApiWebApplicationFactory _factory;

    public ApiIntegrationTests(ApiWebApplicationFactory factory)
    {
        _factory = factory;
    }

    private async Task<HttpClient> AdminClientAsync()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", await _factory.GetTokenAsync("Admin"));
        return client;
    }

    private async Task<HttpClient> CourierClientAsync()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", await _factory.GetTokenAsync("Courier"));
        return client;
    }

    private static object CreateBody(string tracking) => new
    {
        trackingNumber = tracking,
        weightKg = 2.5m,
        recipientName = "Jane Doe",
        recipientPhone = "+15551234567",
        recipientAddress = "123 Main St",
        recipientCity = "Springfield",
        price = 15.99m
    };

    [Fact]
    public async Task MissingToken_Returns401_ProblemJson()
    {
        await _factory.EnsureDatabaseReadyAsync();
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/v2/shipments/APX-12345678");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task CourierCannotCreateOrUpdate_Returns403()
    {
        await _factory.EnsureDatabaseReadyAsync();
        var courier = await CourierClientAsync();
        var admin = await AdminClientAsync();

        var create = await courier.PostAsJsonAsync("/api/v2/shipments", CreateBody("APX-10000001"));
        Assert.Equal(HttpStatusCode.Forbidden, create.StatusCode);

        var update = await courier.PutAsJsonAsync("/api/v2/shipments/APX-10000001", new
        {
            trackingNumber = "APX-10000001", weightKg = 1m, price = 1m,
            recipientName = "A", recipientPhone = "+15550000000",
            recipientAddress = "B", recipientCity = "C"
        });
        Assert.Equal(HttpStatusCode.Forbidden, update.StatusCode);

        // Admin can still create the shipment the courier was blocked from.
        var adminCreate = await admin.PostAsJsonAsync("/api/v2/shipments", CreateBody("APX-10000001"));
        Assert.Equal(HttpStatusCode.Created, adminCreate.StatusCode);
        Assert.NotNull(adminCreate.Headers.Location);
    }

    [Fact]
    public async Task Create_ThenGet_Returns201AndV2Contract()
    {
        await _factory.EnsureDatabaseReadyAsync();
        var admin = await AdminClientAsync();

        var create = await admin.PostAsJsonAsync("/api/v2/shipments", CreateBody("APX-10000002"));

        Assert.Equal(HttpStatusCode.Created, create.StatusCode);
        var body = await create.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("APX-10000002", body.GetProperty("trackingNumber").GetString());
        Assert.True(body.TryGetProperty("recipient", out _));
        Assert.True(body.TryGetProperty("latestScan", out _));
    }

    [Fact]
    public async Task DuplicateCreate_Returns409ProblemDetails()
    {
        await _factory.EnsureDatabaseReadyAsync();
        var admin = await AdminClientAsync();
        await admin.PostAsJsonAsync("/api/v2/shipments", CreateBody("APX-10000003"));

        var duplicate = await admin.PostAsJsonAsync("/api/v2/shipments", CreateBody("APX-10000003"));

        Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);
        var problem = await duplicate.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(409, problem.GetProperty("status").GetInt32());
    }

    [Fact]
    public async Task InvalidPage_Returns400()
    {
        await _factory.EnsureDatabaseReadyAsync();
        var client = await AdminClientAsync();

        var response = await client.GetAsync("/api/v2/shipments?page=0&pageSize=20");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task StatusPatch_ReturnsUpdatedShipmentBody()
    {
        await _factory.EnsureDatabaseReadyAsync();
        var admin = await AdminClientAsync();
        var courier = await CourierClientAsync();
        await admin.PostAsJsonAsync("/api/v2/shipments", CreateBody("APX-10000004"));

        var patch = await courier.PatchAsJsonAsync("/api/v2/shipments/APX-10000004/status", new { status = "PickedUp" });

        Assert.Equal(HttpStatusCode.OK, patch.StatusCode);
        var body = await patch.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("PickedUp", body.GetProperty("status").GetString());
    }

    [Fact]
    public async Task V1Responses_CarryDeprecationAndSunsetHeaders()
    {
        await _factory.EnsureDatabaseReadyAsync();
        var admin = await AdminClientAsync();
        await admin.PostAsJsonAsync("/api/v1/shipments", CreateBody("APX-10000005"));

        var response = await admin.GetAsync("/api/v1/shipments/APX-10000005");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(response.Headers.Contains("api-deprecated-versions"));
        Assert.Equal("Sat, 15 May 2027 00:00:00 GMT", response.Headers.GetValues("Sunset").Single());
    }

    [Fact]
    public async Task PartnerEndpoint_RequiresValidApiKey()
    {
        await _factory.EnsureDatabaseReadyAsync();
        var admin = await AdminClientAsync();
        await admin.PostAsJsonAsync("/api/v2/shipments", CreateBody("APX-10000006"));
        var anonymous = _factory.CreateClient();

        Assert.Equal(HttpStatusCode.Unauthorized,
            (await anonymous.GetAsync("/api/v2/partner/shipments/APX-10000006")).StatusCode);

        var wrongKey = _factory.CreateClient();
        wrongKey.DefaultRequestHeaders.Add("X-Api-Key", "wrong-key");
        Assert.Equal(HttpStatusCode.Unauthorized,
            (await wrongKey.GetAsync("/api/v2/partner/shipments/APX-10000006")).StatusCode);

        var goodKey = _factory.CreateClient();
        goodKey.DefaultRequestHeaders.Add("X-Api-Key", "test-partner-key-123");
        var ok = await goodKey.GetAsync("/api/v2/partner/shipments/APX-10000006");
        Assert.Equal(HttpStatusCode.OK, ok.StatusCode);
        var body = await ok.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("APX-10000006", body.GetProperty("trackingReference").GetString());
    }

    [Fact]
    public async Task OpenApi_V2_DeclaresApiKeyScheme_OnPartnerRouteOnly()
    {
        await _factory.EnsureDatabaseReadyAsync();
        var client = _factory.CreateClient();

        var doc = await client.GetStringAsync("/openapi/v2.json");
        using var json = JsonDocument.Parse(doc);
        var root = json.RootElement;

        Assert.True(root.GetProperty("components").GetProperty("securitySchemes")
            .TryGetProperty("ApiKey", out var apiKey));
        Assert.Equal("apiKey", apiKey.GetProperty("type").GetString());
        Assert.Equal("X-Api-Key", apiKey.GetProperty("name").GetString());

        var partnerOp = root.GetProperty("paths").GetProperty("/api/v2/partner/shipments/{trackingNumber}")
            .GetProperty("get");
        var schemes = partnerOp.GetProperty("security").EnumerateArray().First()
            .EnumerateObject().Select(p => p.Name).ToList();
        Assert.Equal(new[] { "ApiKey" }, schemes);

        // Non-partner operations inherit the document-level Bearer requirement.
        var docSchemes = root.GetProperty("security").EnumerateArray().First()
            .EnumerateObject().Select(p => p.Name).ToList();
        Assert.Equal(new[] { "Bearer" }, docSchemes);
    }
}
