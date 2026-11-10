namespace ApexPost.Api.DTOs.V2;

/// <summary>
/// Minimal read-only shipment view returned to partners via the X-Api-Key endpoint.
/// </summary>
public sealed class PartnerShipmentResponse
{
    /// <summary>Tracking reference in the format APX-XXXXXXXX.</summary>
    public string TrackingReference { get; init; } = string.Empty;

    /// <summary>Current lifecycle status.</summary>
    public string Status { get; init; } = string.Empty;

    /// <summary>UTC time the shipment was created.</summary>
    public DateTimeOffset CreatedAtUtc { get; init; }
}
