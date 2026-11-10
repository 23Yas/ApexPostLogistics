using System.ComponentModel.DataAnnotations;

namespace ApexPost.Api.DTOs;

/// <summary>
/// Request body for changing only the status of a shipment.
/// </summary>
public sealed class UpdateStatusRequest
{
    /// <summary>Target status; must be a known shipment status and a valid transition.</summary>
    [Required(ErrorMessage = "Status is required")]
    public string Status { get; init; } = string.Empty;
}
