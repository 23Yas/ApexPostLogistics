// AI-assisted draft (see COPILOT-LOG.md) - reviewed and kept.
namespace ApexPost.Specs;

using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using ApexPost.Tests;
using Reqnroll;

[Binding]
[Scope(Tag = "shipment-creation")]
public sealed class ShipmentCreationSteps
{
    private readonly ScenarioContext _ctx;

    public ShipmentCreationSteps(ScenarioContext ctx) => _ctx = ctx;

    private ApiWebApplicationFactory Factory => _ctx.Get<ApiWebApplicationFactory>("factory");
    private HttpClient Client => _ctx.Get<HttpClient>("client");
    private HttpResponseMessage? Response => _ctx.Get<HttpResponseMessage?>("response");

    [BeforeScenario]
    public async Task BeforeScenario()
    {
        var factory = new ApiWebApplicationFactory();
        await factory.EnsureDatabaseReadyAsync();
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", await factory.GetTokenAsync("Admin"));

        _ctx["factory"] = factory;
        _ctx["client"] = client;
    }

    [AfterScenario]
    public void AfterScenario()
    {
        _ctx.Get<HttpClient>("client").Dispose();
        _ctx.Get<ApiWebApplicationFactory>("factory").Dispose();
    }

    [Given("the API is running with a fresh test database")]
    public void ApiIsRunning() { /* factory + migration done in BeforeScenario */ }

    [Given("I have an admin token")]
    public void AdminToken() { /* token attached in BeforeScenario */ }

    [When("I create a shipment with tracking number {string} and weight {double} kg")]
    public async Task CreateShipment(string tracking, double weight)
    {
        _ctx["response"] = await Client.PostAsJsonAsync("/api/v2/shipments", new
        {
            trackingNumber = tracking,
            weightKg = weight,
            recipientName = "BDD User",
            recipientPhone = "+15550001111",
            recipientAddress = "1 Spec St",
            recipientCity = "Testville",
            price = 12.50m
        });
    }

    [Given("a shipment {string} already exists")]
    public async Task ShipmentExists(string tracking)
    {
        var response = await Client.PostAsJsonAsync("/api/v2/shipments", new
        {
            trackingNumber = tracking,
            weightKg = 1.0m,
            recipientName = "BDD User",
            recipientPhone = "+15550001111",
            recipientAddress = "1 Spec St",
            recipientCity = "Testville",
            price = 5.00m
        });
        response.EnsureSuccessStatusCode();
    }

    [Then("the response status is {int}")]
    public void ResponseStatusIs(int expected)
        => Assert.Equal((HttpStatusCode)expected, Response!.StatusCode);

    [Then("the response has a Location header")]
    public void ResponseHasLocation()
        => Assert.NotNull(Response!.Headers.Location);

    [Then("the response body tracking number is {string}")]
    public async Task ResponseTrackingNumber(string expected)
    {
        var body = await Response!.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(expected, body.GetProperty("trackingNumber").GetString());
    }

    [Then("the response content type is {string}")]
    public void ResponseContentType(string expected)
        => Assert.Equal(expected, Response!.Content.Headers.ContentType?.MediaType);
}
