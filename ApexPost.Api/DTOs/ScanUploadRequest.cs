using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace ApexPost.Api.DTOs;

public sealed class ScanUploadRequest
{
    public List<ShipmentScanDto> Scans { get; init; } = new();
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ScanType
{
    Pickup,
    InTransit,
    Delivered,
    Failed
}

public sealed class ShipmentScanDto : IValidatableObject
{
    public Guid Id { get; init; }

    [Required]
    public string TrackingNumber { get; init; } = string.Empty;

    [Required]
    public ScanType ScanType { get; init; }

    [Required]
    public string CourierId { get; init; } = string.Empty;

    [Required]
    public string Location { get; init; } = string.Empty;

    [Required]
    public DateTimeOffset ScannedAtUtc { get; init; }

    public string Notes { get; init; } = string.Empty;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        var results = new List<ValidationResult>();

        // Custom validation: scan time cannot be in the future
        if (ScannedAtUtc > DateTimeOffset.UtcNow.AddMinutes(5))
        {
            results.Add(new ValidationResult("Scan time cannot be in the future", new[] { nameof(ScannedAtUtc) }));
        }

        // Custom validation: Delivered scan requires notes (recipient signature)
        if (ScanType == ScanType.Delivered && string.IsNullOrWhiteSpace(Notes))
        {
            results.Add(new ValidationResult("Delivered scan requires recipient signature in notes", new[] { nameof(Notes) }));
        }

        return results;
    }
}
