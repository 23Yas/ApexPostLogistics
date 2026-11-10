using Asp.Versioning;
using ApexPost.Api.DTOs.V2;
using ApexPost.Api.Filters;
using ApexPost.Core.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace ApexPost.Api.Controllers.V2;

/// <summary>
/// Partner read-only API with X-Api-Key header authentication
/// </summary>
[ApiController]
[ApiVersion(2.0)]
[Route("api/v2/partner/shipments")]
[ServiceFilter(typeof(PartnerApiKeyFilter))]
public class PartnerShipmentsController : ControllerBase
{
    private readonly ShipmentService _shipmentService;
    private readonly ILogger<PartnerShipmentsController> _logger;

    public PartnerShipmentsController(
        ShipmentService shipmentService,
        ILogger<PartnerShipmentsController> logger)
    {
        _shipmentService = shipmentService;
        _logger = logger;
    }

    /// <summary>
    /// Get shipment by tracking number (partner API key required)
    /// </summary>
    [HttpGet("{trackingNumber:regex(^APX-\\d{{8}}$)}", Name = "GetPartnerShipment")]
    [Tags("Partner")]
    [EndpointSummary("Partner read-only shipment lookup")]
    [ProducesResponseType(typeof(PartnerShipmentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetShipment(string trackingNumber)
    {
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
        return Ok(new PartnerShipmentResponse
        {
            TrackingReference = shipment.TrackingReference,
            Status = shipment.Status,
            CreatedAtUtc = shipment.CreatedAtUtc
        });
    }
}
