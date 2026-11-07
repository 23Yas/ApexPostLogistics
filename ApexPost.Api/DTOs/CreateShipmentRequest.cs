using System.ComponentModel.DataAnnotations;

namespace ApexPost.Api.DTOs;

public sealed class CreateShipmentRequest
{
    [Required(ErrorMessage = "Tracking number is required")]
    [RegularExpression(@"^APX-\d{8}$", ErrorMessage = "Tracking number must match format APX-XXXXXXXX")]
    public string TrackingNumber { get; init; } = string.Empty;

    [Required(ErrorMessage = "Weight is required")]
    [Range(0.1, 70, ErrorMessage = "Weight must be between 0.1 and 70 kg")]
    public decimal WeightKg { get; init; }

    [Required(ErrorMessage = "Recipient name is required")]
    public string RecipientName { get; init; } = string.Empty;

    [Required(ErrorMessage = "Recipient phone is required")]
    [Phone(ErrorMessage = "Invalid phone number format")]
    public string RecipientPhone { get; init; } = string.Empty;

    [Required(ErrorMessage = "Recipient address is required")]
    public string RecipientAddress { get; init; } = string.Empty;

    [Required(ErrorMessage = "Recipient city is required")]
    public string RecipientCity { get; init; } = string.Empty;

    [Range(0, 100000, ErrorMessage = "Price must be non-negative")]
    public decimal Price { get; init; }
}
