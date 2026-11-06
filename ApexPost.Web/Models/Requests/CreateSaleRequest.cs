namespace ApexPost.Web.Models.Requests;

using System.ComponentModel.DataAnnotations;

/// <summary>
/// The only fields a clerk can post for a sale. Status, Id, clerk, totals and audit fields
/// are deliberately absent, so over-posting them has no effect.
/// </summary>
public sealed class CreateSaleRequest
{
    [Required(ErrorMessage = "Choose a product.")]
    [StringLength(64)]
    public string ProductSku { get; set; } = string.Empty;

    [Range(1, 1000, ErrorMessage = "Quantity must be between 1 and 1000.")]
    public int Quantity { get; set; } = 1;

    [Required(ErrorMessage = "Choose a payment method.")]
    [StringLength(32)]
    public string PaymentMethod { get; set; } = string.Empty;

    [StringLength(32)]
    public string? ServiceLevel { get; set; }

    [StringLength(32)]
    public string? CounterId { get; set; }
}