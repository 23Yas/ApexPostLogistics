using ApexPost.Client;
using Microsoft.Extensions.DependencyInjection;

// Console consumer for the generated ApexPost.Client SDK (built from contracts/v2.json).
// Pass a bearer token via the APEX_TOKEN environment variable or --token arg
// (obtain one from POST /dev/token in Development).
var token = Environment.GetEnvironmentVariable("APEX_TOKEN")
    ?? (args.Length > 1 && args[0] == "--token" ? args[1] : null);

var services = new ServiceCollection();
services.AddHttpClient<Client>(client =>
{
    client.BaseAddress = new Uri("https://localhost:7145"); // dev API base URL
})
.AddHttpMessageHandler(() => new BearerHandler(token));
var provider = services.BuildServiceProvider();

var client = provider.GetRequiredService<Client>();
const string tracking = "APX-12345678";
// ScanType enum values are generated from the contract (Pickup, InTransit, Delivered, Failed).

try
{
    // 1. Create a shipment
    Console.WriteLine("Creating shipment...");
    var created = await client.CreateShipmentV2Async(new CreateShipmentRequest
    {
        TrackingNumber = tracking,
        WeightKg = 2.5,
        RecipientName = "Jane Doe",
        RecipientPhone = "+15551234567",
        RecipientAddress = "123 Main St",
        RecipientCity = "Springfield",
        Price = 15.99
    });
    Console.WriteLine($"Created: {created.TrackingNumber} - Status: {created.Status}");
    Console.WriteLine($"Recipient: {created.Recipient?.Name}, {created.Recipient?.City}");

    // 2. Fetch it
    Console.WriteLine("Fetching shipment...");
    var shipment = await client.GetShipmentV2Async(tracking);
    Console.WriteLine($"Found: {shipment.TrackingNumber} - Status: {shipment.Status}");

    // 3. Upload two scans
    Console.WriteLine("Uploading scans...");
    var upload = await client.UploadScansV2Async(tracking, new ScanUploadRequest
    {
        Scans = new List<ShipmentScanDto>
        {
            new() { Id = Guid.NewGuid(), TrackingNumber = tracking, ScanType = ScanType.Pickup, CourierId = "courier-001", Location = "Depot A", ScannedAtUtc = DateTimeOffset.UtcNow, Notes = "" },
            new() { Id = Guid.NewGuid(), TrackingNumber = tracking, ScanType = ScanType.InTransit, CourierId = "courier-001", Location = "Hub B", ScannedAtUtc = DateTimeOffset.UtcNow, Notes = "" }
        }
    });
    Console.WriteLine($"Accepted: {upload.Accepted}, Duplicates: {upload.DuplicatesSkipped}, Rejected: {upload.Rejected}");
    foreach (var r in upload.Rejections ?? Enumerable.Empty<ScanRejectionDto>())
    {
        Console.WriteLine($"  Rejected {r.ScanId}: {r.Reason}");
    }

    // 4. Handle a 404 through the generated exception type
    try
    {
        await client.GetShipmentV2Async("APX-99999999");
        Console.WriteLine("ERROR: expected 404");
    }
    catch (ApiException ex) when (ex.StatusCode == 404)
    {
        Console.WriteLine($"Shipment not found handled via ApiException (404): {ex.Message}");
    }
}
catch (ApiException ex)
{
    Console.WriteLine($"API error {ex.StatusCode}: {ex.Message}");
}

/// <summary>Attaches the bearer token when one is supplied.</summary>
internal sealed class BearerHandler : DelegatingHandler
{
    private readonly string? _token;
    public BearerHandler(string? token) { _token = token; }
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
    {
        if (!string.IsNullOrEmpty(_token))
        {
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _token);
        }
        return base.SendAsync(request, ct);
    }
}
