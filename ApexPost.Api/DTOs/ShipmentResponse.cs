namespace ApexPost.Api.DTOs;

/// <summary>
/// v1 shipment response. Exposes only the public contract fields - internal
/// linkage such as counter transaction, branch and zone identifiers is hidden.
/// </summary>
public sealed class ShipmentResponse
{
    /// <summary>Unique shipment identifier.</summary>
    public Guid Id { get; init; }

    /// <summary>Tracking reference in the format APX-XXXXXXXX.</summary>
    public string TrackingReference { get; init; } = string.Empty;

    /// <summary>Parcel weight in kilograms.</summary>
    public decimal WeightKg { get; init; }

    /// <summary>Shipping price in the local currency.</summary>
    public decimal Price { get; init; }

    /// <summary>Service level applied to the shipment.</summary>
    public string ServiceLevel { get; init; } = string.Empty;

    /// <summary>Current lifecycle status (Created, PickedUp, InTransit, OutForDelivery, Delivered, Failed).</summary>
    public string Status { get; init; } = string.Empty;

    /// <summary>Full name of the recipient.</summary>
    public string RecipientName { get; init; } = string.Empty;

    /// <summary>Recipient phone number.</summary>
    public string RecipientPhone { get; init; } = string.Empty;

    /// <summary>Street address of the recipient.</summary>
    public string RecipientAddress { get; init; } = string.Empty;

    /// <summary>City of the recipient.</summary>
    public string RecipientCity { get; init; } = string.Empty;

    /// <summary>UTC time the shipment was created.</summary>
    public DateTimeOffset CreatedAtUtc { get; init; }
}

/// <summary>
/// v1 scan response for a single recorded scan event.
/// </summary>
public sealed class ShipmentScanResponse
{
    /// <summary>Unique scan identifier (client-generated, used for de-duplication).</summary>
    public Guid Id { get; init; }

    /// <summary>Tracking number the scan belongs to.</summary>
    public string TrackingNumber { get; init; } = string.Empty;

    /// <summary>Type of scan event (Pickup, InTransit, Delivered, Failed).</summary>
    public string ScanType { get; init; } = string.Empty;

    /// <summary>Courier who performed the scan.</summary>
    public string CourierId { get; init; } = string.Empty;

    /// <summary>Free-text location where the scan was taken.</summary>
    public string Location { get; init; } = string.Empty;

    /// <summary>UTC time the scan was captured.</summary>
    public DateTimeOffset ScannedAtUtc { get; init; }

    /// <summary>Optional notes (recipient signature for Delivered scans).</summary>
    public string Notes { get; init; } = string.Empty;
}
