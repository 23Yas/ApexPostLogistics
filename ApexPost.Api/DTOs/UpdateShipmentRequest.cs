using System.ComponentModel.DataAnnotations;

namespace ApexPost.Api.DTOs;

public sealed class UpdateShipmentRequest
{
    [Required(ErrorMessage = "Tracking number is required")]
    [RegularExpression(@"^APX-\d{8}$", ErrorMessage = "Tracking number must match format APX-XXXXXXXX")]
    public string TrackingNumber { get; init; } = string.Empty;

    [Range(0.1, 70, ErrorMessage = "Weight must be between 0.1 and 70 kg")]
    public decimal WeightKg { get; init; }

    [Range(0, 100000, ErrorMessage = "Price must be non-negative")]
    public decimal Price { get; init; }

    public string? RecipientName { get; init; }
    public string? RecipientPhone { get; init; }
    public string? RecipientAddress { get; init; }
    public string? RecipientCity { get; init; }
}
