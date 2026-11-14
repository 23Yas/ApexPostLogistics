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
    private ApiWebApplicationFactory? _factory;
    private HttpClient? _client;
    private HttpResponseMessage? _response;

    [BeforeScenario]
    public async Task BeforeScenario()
    {
        _factory = new ApiWebApplicationFactory();
        await _factory.EnsureDatabaseReadyAsync();
        _client = _factory.CreateClient();
        var token = await _factory.GetTokenAsync("Admin");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }

    [AfterScenario]
    public void AfterScenario()
    {
        _client?.Dispose();
        _factory?.Dispose();
    }

    [Given("the API is running with a fresh test database")]
    public void ApiIsRunning() { /* factory + migration done in BeforeScenario */ }

    [Given("I have an admin token")]
    public void AdminToken() { /* token attached in BeforeScenario */ }

    [When("I create a shipment with tracking number {string} and weight {double} kg")]
    public async Task CreateShipment(string tracking, double weight)
    {
        _response = await _client!.PostAsJsonAsync("/api/v2/shipments", new
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
        var response = await _client!.PostAsJsonAsync("/api/v2/shipments", new
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
        => Assert.Equal((HttpStatusCode)expected, _response!.StatusCode);

    [Then("the response has a Location header")]
    public void ResponseHasLocation()
        => Assert.NotNull(_response!.Headers.Location);

    [Then("the response body tracking number is {string}")]
    public async Task ResponseTrackingNumber(string expected)
    {
        var body = await _response!.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(expected, body.GetProperty("trackingNumber").GetString());
    }

    [Then("the response content type is {string}")]
    public void ResponseContentType(string expected)
        => Assert.Equal(expected, _response!.Content.Headers.ContentType?.MediaType);
}
