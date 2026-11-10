using System.ComponentModel.DataAnnotations;

namespace ApexPost.Api.DTOs;

/// <summary>
/// Request body for replacing the editable details of an existing shipment.
/// </summary>
public sealed class UpdateShipmentRequest
{
    /// <summary>Tracking number; must match the tracking number in the route.</summary>
    [Required(ErrorMessage = "Tracking number is required")]
    [RegularExpression(@"^APX-\d{8}$", ErrorMessage = "Tracking number must match format APX-XXXXXXXX")]
    public string TrackingNumber { get; init; } = string.Empty;

    /// <summary>Parcel weight in kilograms (0.1 - 70).</summary>
    [Range(0.1, 70, ErrorMessage = "Weight must be between 0.1 and 70 kg")]
    public decimal WeightKg { get; init; }

    /// <summary>Shipping price in the local currency (non-negative).</summary>
    [Range(0, 100000, ErrorMessage = "Price must be non-negative")]
    public decimal Price { get; init; }

    /// <summary>Full name of the recipient.</summary>
    [Required(ErrorMessage = "Recipient name is required")]
    [StringLength(200)]
    public string RecipientName { get; init; } = string.Empty;

    /// <summary>Recipient phone number in a valid international format.</summary>
    [Required(ErrorMessage = "Recipient phone is required")]
    [Phone(ErrorMessage = "Recipient phone must be a valid phone number")]
    [StringLength(32)]
    public string RecipientPhone { get; init; } = string.Empty;

    /// <summary>Street address of the recipient.</summary>
    [Required(ErrorMessage = "Recipient address is required")]
    [StringLength(400)]
    public string RecipientAddress { get; init; } = string.Empty;

    /// <summary>City of the recipient.</summary>
    [Required(ErrorMessage = "Recipient city is required")]
    [StringLength(100)]
    public string RecipientCity { get; init; } = string.Empty;
}
