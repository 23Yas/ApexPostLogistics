using System.ComponentModel.DataAnnotations;

namespace ApexPost.Api.DTOs;

public sealed class UpdateStatusRequest
{
    [Required(ErrorMessage = "Status is required")]
    public string Status { get; init; } = string.Empty;
}
