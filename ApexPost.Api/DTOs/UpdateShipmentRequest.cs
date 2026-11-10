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

    [Required(ErrorMessage = "Recipient name is required")]
    [StringLength(200)]
    public string RecipientName { get; init; } = string.Empty;

    [Required(ErrorMessage = "Recipient phone is required")]
    [Phone(ErrorMessage = "Recipient phone must be a valid phone number")]
    [StringLength(32)]
    public string RecipientPhone { get; init; } = string.Empty;

    [Required(ErrorMessage = "Recipient address is required")]
    [StringLength(400)]
    public string RecipientAddress { get; init; } = string.Empty;

    [Required(ErrorMessage = "Recipient city is required")]
    [StringLength(100)]
    public string RecipientCity { get; init; } = string.Empty;
}
