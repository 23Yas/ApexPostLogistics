using ApexPost.Client;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http;

// Console consumer for the generated ApexPost.Client SDK
var services = new ServiceCollection();
services.AddHttpClient<ApexPostClient>(client =>
{
    client.BaseAddress = new Uri("https://localhost:7xxx"); // dev API base URL
});
var provider = services.BuildServiceProvider();

var client = provider.GetRequiredService<ApexPostClient>();

try
{
    // 1. Create a shipment
    var createRequest = new CreateShipmentRequest
    {
        TrackingNumber = "APX-12345678",
        WeightKg = 2.5m,
        RecipientName = "Jane Doe",
        RecipientPhone = "+15551234567",
        RecipientAddress = "123 Main St",
        RecipientCity = "Springfield",
        Price = 15.99m
    };

    Console.WriteLine("Creating shipment...");
    // var created = await client.CreateShipmentAsync(createRequest);
    // Console.WriteLine($"Created: {created.TrackingNumber}");

    // 2. Fetch it
    Console.WriteLine("Fetching shipment...");
    // var shipment = await client.GetShipmentAsync("APX-12345678");
    // Console.WriteLine($"Found: {shipment.TrackingNumber} - Status: {shipment.Status}");

    // 3. Upload two scans
    Console.WriteLine("Uploading scans...");
    // var scans = new ScanUploadRequest
    // {
    //     Scans = new List<ShipmentScanDto>
    //     {
    //         new() { Id = Guid.NewGuid(), TrackingNumber = "APX-12345678", ScanType = ScanType.Pickup, CourierId = "courier-001", Location = "Depot A", ScannedAtUtc = DateTimeOffset.UtcNow, Notes = "" },
    //         new() { Id = Guid.NewGuid(), TrackingNumber = "APX-12345678", ScanType = ScanType.InTransit, CourierId = "courier-001", Location = "Hub B", ScannedAtUtc = DateTimeOffset.UtcNow, Notes = "" }
    //     }
    // };
    // var uploadResult = await client.UploadScansAsync("APX-12345678", scans);
    // Console.WriteLine($"Accepted: {uploadResult.Accepted}, Duplicates: {uploadResult.DuplicatesSkipped}, Rejected: {uploadResult.Rejected}");

    // 4. Handle a 404 through the generated exception type
    try
    {
        // await client.GetShipmentAsync("APX-99999999");
        Console.WriteLine("404 handling would occur here via ApiException");
    }
    catch (ApiException ex) when (ex.StatusCode == 404)
    {
        Console.WriteLine($"Shipment not found (404): {ex.Message}");
    }
}
catch (ApiException ex)
{
    Console.WriteLine($"API error {ex.StatusCode}: {ex.Message}");
}
