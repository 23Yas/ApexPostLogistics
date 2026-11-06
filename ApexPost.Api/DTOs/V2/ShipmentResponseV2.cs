namespace ApexPost.Api.DTOs.V2;

public sealed class ShipmentResponseV2
{
    public Guid Id { get; init; }
    public string TrackingNumber { get; init; } = string.Empty;
    public decimal WeightKg { get; init; }
    public decimal Price { get; init; }
    public string Status { get; init; } = string.Empty;
    public RecipientDto Recipient { get; init; } = new();
    public LatestScanDto? LatestScan { get; init; }
    public DateTimeOffset CreatedAtUtc { get; init; }
}

public sealed class RecipientDto
{
    public string Name { get; init; } = string.Empty;
    public string Phone { get; init; } = string.Empty;
    public string Address { get; init; } = string.Empty;
    public string City { get; init; } = string.Empty;
}

public sealed class LatestScanDto
{
    public string ScanType { get; init; } = string.Empty;
    public string Location { get; init; } = string.Empty;
    public DateTimeOffset ScannedAtUtc { get; init; }
}
