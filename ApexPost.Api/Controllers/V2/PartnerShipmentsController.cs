using ApexPost.Core.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace ApexPost.Api.Controllers.V2;

/// <summary>
/// Partner read-only API with X-Api-Key header authentication
/// </summary>
[ApiController]
[Route("api/v2/partner/shipments")]
public class PartnerShipmentsController : ControllerBase
{
    private readonly ShipmentService _shipmentService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<PartnerShipmentsController> _logger;

    public PartnerShipmentsController(
        ShipmentService shipmentService,
        IConfiguration configuration,
        ILogger<PartnerShipmentsController> logger)
    {
        _shipmentService = shipmentService;
        _configuration = configuration;
        _logger = logger;
    }

    /// <summary>
    /// Get shipment by tracking number (partner API key required)
    /// </summary>
    [HttpGet("{trackingNumber}", Name = "GetPartnerShipment")]
    [Tags("Partner")]
    [EndpointSummary("Partner read-only shipment lookup")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetShipment(string trackingNumber)
    {
        // Validate X-Api-Key header
        var apiKey = Request.Headers["X-Api-Key"].FirstOrDefault();
        var validKeys = _configuration.GetSection("PartnerApiKeys").Get<string[]>() ?? Array.Empty<string>();

        if (string.IsNullOrEmpty(apiKey) || !validKeys.Contains(apiKey))
        {
            return Unauthorized(new ProblemDetails
            {
                Title = "Invalid API key",
                Detail = "Missing or invalid X-Api-Key header",
                Status = StatusCodes.Status401Unauthorized
            });
        }

        var shipment = await _shipmentService.GetByTrackingNumberAsync(trackingNumber);
        if (shipment == null)
        {
            return NotFound(new ProblemDetails
            {
                Title = "Shipment not found",
                Detail = $"No shipment found with tracking number {trackingNumber}",
                Status = StatusCodes.Status404NotFound
            });
        }

        // Return minimal read-only view for partners
        return Ok(new
        {
            shipment.TrackingReference,
            shipment.Status,
            shipment.CreatedAtUtc
        });
    }
}
