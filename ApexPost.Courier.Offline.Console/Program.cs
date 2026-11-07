using ApexPost.Courier.Offline;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

var serviceProvider = new ServiceCollection()
    .AddLogging(builder => builder.AddConsole())
    .AddSingleton<CourierDatabase>(sp => new CourierDatabase("apexpost_courier", sp.GetRequiredService<ILogger<CourierDatabase>>()))
    .AddSingleton<IScanRepository, CouchbaseScanRepository>()
    .AddSingleton<IShipmentUploader, FakeShipmentUploader>()
    .AddSingleton<OutboxService>()
    .BuildServiceProvider();

var logger = serviceProvider.GetRequiredService<ILogger<Program>>();
var database = serviceProvider.GetRequiredService<CourierDatabase>();
var repository = serviceProvider.GetRequiredService<IScanRepository>();
var outboxService = serviceProvider.GetRequiredService<OutboxService>();

// Initialize database and create indexes
database.CreateIndexes();

// Register change listener to track pending count
int pendingCount = 0;
database.RegisterChangeListener(changedIds =>
{
    Console.WriteLine($"[Listener] Changed document IDs: {string.Join(", ", changedIds)}");
    // Refresh pending count when documents change
    Task.Run(async () =>
    {
        var pending = await repository.GetPendingScansAsync();
        pendingCount = pending.Count;
        Console.WriteLine($"\n[Pending scans updated: {pendingCount}]");
    });
});

// Initialize pending count
pendingCount = (await repository.GetPendingScansAsync()).Count;

logger.LogInformation("ApexPost Courier Offline Console - Ready");
logger.LogInformation("Pending scans: {Count}", pendingCount);

while (true)
{
    Console.WriteLine("\n=== ApexPost Courier Offline ===");
    Console.WriteLine($"Pending Scans: {pendingCount}");
    Console.WriteLine("1. Add Scan");
    Console.WriteLine("2. Find by Tracking Number");
    Console.WriteLine("3. List Pending");
    Console.WriteLine("4. Simulate Sync");
    Console.WriteLine("5. Delete Scan");
    Console.WriteLine("6. Exit");
    Console.Write("Select option: ");

    var choice = Console.ReadLine();

    try
    {
        switch (choice)
        {
            case "1":
                await AddScan(repository);
                pendingCount = (await repository.GetPendingScansAsync()).Count;
                break;
            case "2":
                await FindByTrackingNumber(repository);
                break;
            case "3":
                await ListPending(repository);
                break;
            case "4":
                await SimulateSync(outboxService);
                pendingCount = (await repository.GetPendingScansAsync()).Count;
                break;
            case "5":
                await DeleteScan(repository);
                pendingCount = (await repository.GetPendingScansAsync()).Count;
                break;
            case "6":
                logger.LogInformation("Exiting...");
                database.Dispose();
                return;
            default:
                Console.WriteLine("Invalid option. Please try again.");
                break;
        }
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error executing option");
        Console.WriteLine($"Error: {ex.Message}");
    }
}

static async Task AddScan(IScanRepository repository)
{
    Console.WriteLine("\n--- Add New Scan ---");
    Console.Write("Tracking Number: ");
    var trackingNumber = Console.ReadLine() ?? string.Empty;

    Console.Write("Scan Type (Pickup/InTransit/Delivered/Failed): ");
    var scanTypeStr = Console.ReadLine() ?? "Pickup";
    var scanType = Enum.Parse<ScanType>(scanTypeStr);

    Console.Write("Courier ID: ");
    var courierId = Console.ReadLine() ?? "courier-001";

    Console.Write("Location: ");
    var location = Console.ReadLine() ?? "Unknown";

    Console.Write("Notes: ");
    var notes = Console.ReadLine() ?? string.Empty;

    var scan = new ShipmentScan
    {
        TrackingNumber = trackingNumber,
        ScanType = scanType,
        CourierId = courierId,
        Location = location,
        ScannedAtUtc = DateTimeOffset.UtcNow,
        Notes = notes,
        SyncStatus = SyncStatus.Pending
    };

    await repository.CreateAsync(scan);
    Console.WriteLine($"Scan created with ID: {scan.Id}");
}

static async Task FindByTrackingNumber(IScanRepository repository)
{
    Console.WriteLine("\n--- Find by Tracking Number ---");
    Console.Write("Enter Tracking Number: ");
    var trackingNumber = Console.ReadLine() ?? string.Empty;

    var scans = await repository.GetByTrackingNumberAsync(trackingNumber);

    if (scans.Count == 0)
    {
        Console.WriteLine("No scans found for this tracking number.");
    }
    else
    {
        Console.WriteLine($"Found {scans.Count} scan(s):");
        foreach (var scan in scans)
        {
            Console.WriteLine($"  - ID: {scan.Id}");
            Console.WriteLine($"    Type: {scan.ScanType}");
            Console.WriteLine($"    Location: {scan.Location}");
            Console.WriteLine($"    Scanned At: {scan.ScannedAtUtc:u}");
            Console.WriteLine($"    Sync Status: {scan.SyncStatus}");
            Console.WriteLine($"    Notes: {scan.Notes}");
            Console.WriteLine();
        }
    }
}

static async Task ListPending(IScanRepository repository)
{
    Console.WriteLine("\n--- Pending Scans ---");
    var pendingScans = await repository.GetPendingScansAsync();

    if (pendingScans.Count == 0)
    {
        Console.WriteLine("No pending scans.");
    }
    else
    {
        Console.WriteLine($"Found {pendingScans.Count} pending scan(s):");
        foreach (var scan in pendingScans)
        {
            Console.WriteLine($"  - ID: {scan.Id}");
            Console.WriteLine($"    Tracking: {scan.TrackingNumber}");
            Console.WriteLine($"    Type: {scan.ScanType}");
            Console.WriteLine($"    Courier: {scan.CourierId}");
            Console.WriteLine($"    Location: {scan.Location}");
            Console.WriteLine($"    Scanned At: {scan.ScannedAtUtc:u}");
            Console.WriteLine();
        }
    }
}

static async Task SimulateSync(OutboxService outboxService)
{
    Console.WriteLine("\n--- Simulate Sync ---");
    Console.WriteLine("Syncing pending scans...");

    var summary = await outboxService.SyncPendingScansAsync();

    Console.WriteLine($"Sync complete:");
    Console.WriteLine($"  Total Pending: {summary.TotalPending}");
    Console.WriteLine($"  Synced: {summary.Synced}");
    Console.WriteLine($"  Failed: {summary.Failed}");
}

static async Task DeleteScan(IScanRepository repository)
{
    Console.WriteLine("\n--- Delete Scan ---");
    Console.Write("Enter Scan ID (Guid): ");
    var idStr = Console.ReadLine() ?? string.Empty;

    if (Guid.TryParse(idStr, out var id))
    {
        await repository.DeleteAsync(id);
        Console.WriteLine($"Scan {id} deleted.");
    }
    else
    {
        Console.WriteLine("Invalid Guid format.");
    }
}
