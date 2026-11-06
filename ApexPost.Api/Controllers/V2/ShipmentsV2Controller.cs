using ApexPost.Api.DTOs;
using ApexPost.Api.DTOs.V2;
using ApexPost.Core.Application.Services;
using ApexPost.Core.Domain;
using Microsoft.AspNetCore.Mvc;

namespace ApexPost.Api.Controllers.V2;

/// <summary>
/// Shipments API v2 controller with enhanced response structure
/// </summary>
[ApiController]
[Route("api/v2/[controller]")]
public class ShipmentsV2Controller : ControllerBase
{
    private readonly ShipmentService _shipmentService;
    private readonly ILogger<ShipmentsV2Controller> _logger;

    public ShipmentsV2Controller(ShipmentService shipmentService, ILogger<ShipmentsV2Controller> logger)
    {
        _shipmentService = shipmentService;
        _logger = logger;
    }

    /// <summary>
    /// Get paged list of shipments with optional filtering (v2)
    /// </summary>
    [HttpGet(Name = "GetShipmentsV2")]
    [Tags("Shipments")]
    [EndpointSummary("Get paged shipments (v2)")]
    [ProducesResponseType(typeof(IEnumerable<ShipmentResponseV2>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetShipments(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? status = null,
        [FromQuery] string? city = null)
    {
        if (pageSize < 1 || pageSize > 50)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Invalid page size",
                Detail = "Page size must be between 1 and 50",
                Status = StatusCodes.Status400BadRequest
            });
        }

        var shipments = await _shipmentService.GetPagedAsync(page, pageSize, status, city);
        var v2Responses = shipments.Select(s => MapToV2Response(s));
        return Ok(v2Responses);
    }

    /// <summary>
    /// Get a single shipment by tracking number (v2)
    /// </summary>
    [HttpGet("{trackingNumber}", Name = "GetShipmentV2")]
    [Tags("Shipments")]
    [EndpointSummary("Get shipment by tracking number (v2)")]
    [ProducesResponseType(typeof(ShipmentResponseV2), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
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

        return Ok(MapToV2Response(shipment));
    }

    /// <summary>
    /// Create a new shipment (v2)
    /// </summary>
    [HttpPost(Name = "CreateShipmentV2")]
    [Tags("Shipments")]
    [EndpointSummary("Create a new shipment (v2)")]
    [ProducesResponseType(typeof(ShipmentResponseV2), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateShipment([FromBody] CreateShipmentRequest request)
    {
        var existing = await _shipmentService.GetByTrackingNumberAsync(request.TrackingNumber);
        if (existing != null)
        {
            return Conflict(new ProblemDetails
            {
                Title = "Duplicate tracking number",
                Detail = $"A shipment with tracking number {request.TrackingNumber} already exists",
                Status = StatusCodes.Status409Conflict
            });
        }

        var shipment = new Shipment
        {
            Id = Guid.NewGuid(),
            TrackingReference = request.TrackingNumber,
            WeightKg = request.WeightKg,
            Price = request.Price,
            Status = "Created",
            CreatedAtUtc = DateTimeOffset.UtcNow
        };

        var created = await _shipmentService.CreateAsync(shipment);
        return CreatedAtAction(nameof(GetShipment), new { trackingNumber = created.TrackingReference }, MapToV2Response(created));
    }

    private static ShipmentResponseV2 MapToV2Response(Shipment shipment)
    {
        return new ShipmentResponseV2
        {
            Id = shipment.Id,
            TrackingNumber = shipment.TrackingReference,
            WeightKg = shipment.WeightKg,
            Price = shipment.Price,
            Status = shipment.Status,
            Recipient = new RecipientDto
            {
                Name = "Unknown", // Would be populated from recipient data in real implementation
                Phone = "Unknown",
                Address = "Unknown",
                City = "Unknown"
            },
            LatestScan = null, // Would be populated from scan data in real implementation
            CreatedAtUtc = shipment.CreatedAtUtc
        };
    }
}
