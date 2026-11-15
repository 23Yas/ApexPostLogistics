// AI-assisted draft (see COPILOT-LOG.md) - reviewed and corrected.
namespace ApexPost.Specs;

using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using ApexPost.Tests;
using Reqnroll;

[Binding]
[Scope(Tag = "scan-sync")]
public sealed class ScanSyncSteps
{
    private readonly ScenarioContext _ctx;

    public ScanSyncSteps(ScenarioContext ctx) => _ctx = ctx;

    private ApiWebApplicationFactory Factory => _ctx.Get<ApiWebApplicationFactory>("factory");
    private HttpClient Admin => _ctx.Get<HttpClient>("admin");
    private HttpClient? Courier => _ctx.Get<HttpClient?>("courier");
    private string CourierId => _ctx.Get<string>("courierId");
    private HttpResponseMessage? Response => _ctx.Get<HttpResponseMessage?>("response");
    private JsonElement UploadResult => _ctx.Get<JsonElement>("uploadResult");

    [BeforeScenario]
    public async Task BeforeScenario()
    {
        var factory = new ApiWebApplicationFactory();
        await factory.EnsureDatabaseReadyAsync();
        var admin = factory.CreateClient();
        admin.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", await factory.GetTokenAsync("Admin"));

        _ctx["factory"] = factory;
        _ctx["admin"] = admin;
    }

    [AfterScenario]
    public void AfterScenario()
    {
        if (_ctx.TryGetValue<HttpClient?>("courier", out var courier)) courier?.Dispose();
        _ctx.Get<HttpClient>("admin").Dispose();
        _ctx.Get<ApiWebApplicationFactory>("factory").Dispose();
    }

    [Given("the API is running with a fresh test database")]
    public void ApiIsRunning() { }

    [Given("I have an admin token")]
    public void AdminToken() { }

    [Given("I have a courier token for {string}")]
    public async Task CourierToken(string courierId)
    {
        var courier = Factory.CreateClient();
        courier.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", await Factory.GetTokenAsync("Courier", courierId));

        _ctx["courier"] = courier;
        _ctx["courierId"] = courierId;
    }

    [Given("a shipment {string} exists in status {string}")]
    public async Task ShipmentInStatus(string tracking, string status)
    {
        await CreateShipmentAsync(tracking);
        // PATCH /status is Courier-only, so transitions go through the courier client.
        var statusClient = Courier ?? Admin;
        foreach (var step in StepsTo(status))
        {
            var patch = await statusClient.PatchAsJsonAsync($"/api/v2/shipments/{tracking}/status", new { status = step });
            patch.EnsureSuccessStatusCode();
        }
    }

    private static readonly Dictionary<string, string[]> Paths = new()
    {
        ["Created"] = Array.Empty<string>(),
        ["PickedUp"] = new[] { "PickedUp" },
        ["InTransit"] = new[] { "PickedUp", "InTransit" },
        ["OutForDelivery"] = new[] { "PickedUp", "InTransit", "OutForDelivery" },
        ["Delivered"] = new[] { "PickedUp", "InTransit", "OutForDelivery", "Delivered" }
    };

    private static IEnumerable<string> StepsTo(string status) => Paths[status];

    private async Task CreateShipmentAsync(string tracking)
    {
        var response = await Admin.PostAsJsonAsync("/api/v2/shipments", new
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

    private async Task UploadScanAsync(string scanId, string tracking, string scanType, string? scanTrackingOverride = null)
    {
        var response = await Courier!.PostAsJsonAsync($"/api/v2/shipments/{tracking}/scans", new
        {
            scans = new[]
            {
                new
                {
                    id = scanId,
                    trackingNumber = scanTrackingOverride ?? tracking,
                    scanType,
                    courierId = CourierId,
                    location = "Depot B",
                    scannedAtUtc = DateTimeOffset.UtcNow.AddMinutes(-5),
                    // Delivered scans must carry a recipient signature in notes (API validation rule).
                    notes = scanType == "Delivered" ? "signature:BDD-Recipient" : ""
                }
            }
        });
        response.EnsureSuccessStatusCode();
        _ctx["response"] = response;
        _ctx["uploadResult"] = await response.Content.ReadFromJsonAsync<JsonElement>();
    }

    [When("I upload a {string} scan for {string} as courier {string}")]
    public async Task UploadScan(string scanType, string tracking, string courier)
    {
        Assert.Equal(CourierId, courier);
        await UploadScanAsync(Guid.NewGuid().ToString(), tracking, scanType);
    }

    [When("I upload a {string} scan with tracking {string} to shipment {string} as courier {string}")]
    public async Task UploadMismatchedScan(string scanType, string scanTracking, string shipmentTracking, string courier)
    {
        Assert.Equal(CourierId, courier);
        await UploadScanAsync(Guid.NewGuid().ToString(), shipmentTracking, scanType, scanTracking);
    }

    [Given("I have already uploaded scan {string} for {string}")]
    public async Task AlreadyUploaded(string scanId, string tracking)
        => await UploadScanAsync(scanId, tracking, "InTransit");

    [When("I upload the same scan {string} again")]
    public async Task UploadSameScanAgain(string scanId)
        => await UploadScanAsync(scanId, "APX-71000001", "InTransit");

    [Then("the upload response accepted count is {int}")]
    public void AcceptedCount(int expected)
        => Assert.Equal(expected, UploadResult.GetProperty("accepted").GetInt32());

    [Then("the upload response duplicates skipped count is {int}")]
    public void DuplicatesSkippedCount(int expected)
        => Assert.Equal(expected, UploadResult.GetProperty("duplicatesSkipped").GetInt32());

    [Then("the upload response rejected count is {int}")]
    public void RejectedCount(int expected)
        => Assert.Equal(expected, UploadResult.GetProperty("rejected").GetInt32());

    [Then("the upload response {string} count is {int}")]
    public void OutcomeCount(string outcome, int expected)
    {
        var property = outcome == "accepted" ? "accepted" : "rejected";
        Assert.Equal(expected, UploadResult.GetProperty(property).GetInt32());
    }

    [Then("the shipment {string} status is {string}")]
    public async Task ShipmentStatusIs(string tracking, string expected)
    {
        var response = await Admin.GetAsync($"/api/v2/shipments/{tracking}");
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(expected, body.GetProperty("status").GetString());
    }
}
