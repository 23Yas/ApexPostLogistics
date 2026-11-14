using Asp.Versioning;
using ApexPost.Api.DTOs;
using ApexPost.Core.Application.Services;
using ApexPost.Core.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ScanType = ApexPost.Api.DTOs.ScanType;
using DomainScanType = ApexPost.Core.Domain.ScanType;

namespace ApexPost.Api.Controllers;

/// <summary>
/// Shipments API controller for managing shipment lifecycle
/// </summary>
[ApiController]
[ApiVersion(1.0, Deprecated = true)]
[Route("api/v{version:apiVersion}/shipments")]
[Authorize]
public class ShipmentsController : ControllerBase
{
    private readonly ShipmentService _shipmentService;
    private readonly ILogger<ShipmentsController> _logger;

    public ShipmentsController(ShipmentService shipmentService, ILogger<ShipmentsController> logger)
    {
        _shipmentService = shipmentService;
        _logger = logger;
    }

    /// <summary>
    /// Get paged list of shipments with optional filtering
    /// </summary>
    /// <param name="page">Page number (default: 1)</param>
    /// <param name="pageSize">Page size (default: 20, max: 50)</param>
    /// <param name="status">Optional status filter</param>
    /// <param name="city">Optional city filter</param>
    /// <returns>Paged list of shipments</returns>
    [HttpGet(Name = "GetShipments")]
    [Tags("Shipments")]
    [EndpointSummary("Get paged shipments")]
    [ProducesResponseType(typeof(IEnumerable<ShipmentResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetShipments(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? status = null,
        [FromQuery] string? city = null)
    {
        var shipments = await _shipmentService.GetPagedAsync(page, pageSize, status, city);
        return Ok(shipments.Select(ToResponse));
    }

    /// <summary>
    /// Get a single shipment by tracking number
    /// </summary>
    /// <param name="trackingNumber">The tracking number of the shipment</param>
    /// <returns>Shipment details</returns>
    [HttpGet("{trackingNumber:regex(^APX-\\d{{8}}$)}", Name = "GetShipment")]
    [Tags("Shipments")]
    [EndpointSummary("Get shipment by tracking number")]
    [ProducesResponseType(typeof(ShipmentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetShipment(string trackingNumber)
    {
        var shipment = await _shipmentService.GetShipmentAsync(trackingNumber);
        return Ok(ToResponse(shipment));
    }

    /// <summary>
    /// Create a new shipment (Admin only)
    /// </summary>
    /// <param name="request">Shipment creation request</param>
    /// <returns>Created shipment with Location header</returns>
    [HttpPost(Name = "CreateShipment")]
    [Authorize(Roles = "Admin")]
    [Tags("Shipments")]
    [EndpointSummary("Create a new shipment")]
    [ProducesResponseType(typeof(ShipmentResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status415UnsupportedMediaType)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> CreateShipment([FromBody] CreateShipmentRequest request)
    {
        var shipment = new Shipment
        {
            Id = Guid.NewGuid(),
            TrackingReference = request.TrackingNumber,
            WeightKg = request.WeightKg,
            Price = request.Price,
            Status = "Created",
            RecipientName = request.RecipientName,
            RecipientPhone = request.RecipientPhone,
            RecipientAddress = request.RecipientAddress,
            RecipientCity = request.RecipientCity,
            CreatedAtUtc = DateTimeOffset.UtcNow
        };

        var created = await _shipmentService.CreateAsync(shipment);
        return CreatedAtAction(nameof(GetShipment), new { trackingNumber = created.TrackingReference }, ToResponse(created));
    }

    /// <summary>
    /// Update editable shipment details (Admin only)
    /// </summary>
    /// <param name="trackingNumber">The tracking number of the shipment</param>
    /// <param name="request">Shipment update request</param>
    /// <returns>Updated shipment</returns>
    [HttpPut("{trackingNumber:regex(^APX-\\d{{8}}$)}", Name = "UpdateShipment")]
    [Authorize(Roles = "Admin")]
    [Tags("Shipments")]
    [EndpointSummary("Update shipment details")]
    [ProducesResponseType(typeof(ShipmentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status415UnsupportedMediaType)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> UpdateShipment(string trackingNumber, [FromBody] UpdateShipmentRequest request)
    {
        if (request.TrackingNumber != trackingNumber)
        {
            throw new BadRequestException("Tracking number in URL must match request body");
        }

        var shipment = new Shipment
        {
            TrackingReference = request.TrackingNumber,
            WeightKg = request.WeightKg,
            Price = request.Price,
            RecipientName = request.RecipientName ?? string.Empty,
            RecipientPhone = request.RecipientPhone ?? string.Empty,
            RecipientAddress = request.RecipientAddress ?? string.Empty,
            RecipientCity = request.RecipientCity ?? string.Empty
        };

        var updated = await _shipmentService.UpdateAsync(trackingNumber, shipment);
        return Ok(ToResponse(updated));
    }

    /// <summary>
    /// Update shipment status only
    /// </summary>
    /// <param name="trackingNumber">The tracking number of the shipment</param>
    /// <param name="request">Status update request</param>
    /// <returns>Updated shipment</returns>
    [HttpPatch("{trackingNumber:regex(^APX-\\d{{8}}$)}/status", Name = "UpdateShipmentStatus")]
    [Authorize(Roles = "Courier")]
    [Tags("Shipments")]
    [EndpointSummary("Update shipment status")]
    [ProducesResponseType(typeof(ShipmentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status415UnsupportedMediaType)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> UpdateStatus(string trackingNumber, [FromBody] UpdateStatusRequest request)
    {
        if (!ShipmentStatusRules.IsKnownStatus(request.Status))
        {
            throw new BadRequestException($"'{request.Status}' is not a valid shipment status");
        }

        var updated = await _shipmentService.UpdateStatusAsync(trackingNumber, request.Status);
        return Ok(ToResponse(updated));
    }

    /// <summary>
    /// Delete a shipment (only allowed when status is Created)
    /// </summary>
    /// <param name="trackingNumber">The tracking number of the shipment</param>
    /// <returns>No content on success</returns>
    [HttpDelete("{trackingNumber:regex(^APX-\\d{{8}}$)}", Name = "DeleteShipment")]
    [Authorize(Roles = "Admin")]
    [Tags("Shipments")]
    [EndpointSummary("Delete a shipment")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> DeleteShipment(string trackingNumber)
    {
        await _shipmentService.DeleteAsync(trackingNumber);
        return NoContent();
    }

    /// <summary>
    /// Bulk upload of offline scans
    /// </summary>
    /// <param name="trackingNumber">The tracking number of the shipment</param>
    /// <param name="request">Scan upload request with list of scans</param>
    /// <returns>Upload summary with accepted, duplicates skipped, and rejected counts</returns>
    [HttpPost("{trackingNumber:regex(^APX-\\d{{8}}$)}/scans", Name = "UploadScans")]
    [Authorize(Roles = "Courier")]
    [Tags("Scans")]
    [EndpointSummary("Bulk upload shipment scans")]
    [ProducesResponseType(typeof(ScanUploadResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status415UnsupportedMediaType)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> UploadScans(string trackingNumber, [FromBody] ScanUploadRequest request)
    {
        // Courier may upload scans only under their own courierId claim
        var courierIdClaim = User.FindFirst("courierId")?.Value;
        if (string.IsNullOrEmpty(courierIdClaim))
        {
            return StatusCode(StatusCodes.Status403Forbidden, new ProblemDetails
            {
                Title = "Forbidden",
                Detail = "A courierId claim is required to upload scans",
                Status = StatusCodes.Status403Forbidden
            });
        }

        if (request.Scans.Any(s => !string.Equals(s.CourierId, courierIdClaim, StringComparison.OrdinalIgnoreCase)))
        {
            return StatusCode(StatusCodes.Status403Forbidden, new ProblemDetails
            {
                Title = "Forbidden",
                Detail = "Scans may only be uploaded under your own courierId",
                Status = StatusCodes.Status403Forbidden
            });
        }

        // Convert DTOs to domain objects
        var scans = request.Scans.Select(dto => new ShipmentScan
        {
            Id = dto.Id,
            TrackingNumber = dto.TrackingNumber,
            ScanType = MapScanType(dto.ScanType),
            CourierId = dto.CourierId,
            Location = dto.Location,
            ScannedAtUtc = dto.ScannedAtUtc,
            Notes = dto.Notes
        }).ToList();

        var outcome = await _shipmentService.UploadScansAsync(trackingNumber, scans);

        var response = new ScanUploadResponse
        {
            Accepted = outcome.Accepted,
            DuplicatesSkipped = outcome.DuplicatesSkipped,
            Rejected = outcome.Rejected,
            Rejections = outcome.Rejections
                .Select(r => new ScanRejectionDto { ScanId = r.ScanId, Reason = r.Reason })
                .ToList()
        };

        return Ok(response);
    }

    /// <summary>
    /// Get scan history for a shipment
    /// </summary>
    /// <param name="trackingNumber">The tracking number of the shipment</param>
    /// <returns>List of scans for the shipment</returns>
    [HttpGet("{trackingNumber:regex(^APX-\\d{{8}}$)}/scans", Name = "GetScans")]
    [Tags("Scans")]
    [EndpointSummary("Get shipment scan history")]
    [ProducesResponseType(typeof(IEnumerable<ShipmentScanResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetScans(string trackingNumber)
    {
        var scans = await _shipmentService.GetScansAsync(trackingNumber);
        return Ok(scans.Select(ToScanResponse));
    }

    private static ShipmentResponse ToResponse(Shipment shipment) => new()
    {
        Id = shipment.Id,
        TrackingReference = shipment.TrackingReference,
        WeightKg = shipment.WeightKg,
        Price = shipment.Price,
        ServiceLevel = shipment.ServiceLevel.ToString(),
        Status = shipment.Status,
        RecipientName = shipment.RecipientName,
        RecipientPhone = shipment.RecipientPhone,
        RecipientAddress = shipment.RecipientAddress,
        RecipientCity = shipment.RecipientCity,
        CreatedAtUtc = shipment.CreatedAtUtc
    };

    private static ShipmentScanResponse ToScanResponse(ShipmentScan scan) => new()
    {
        Id = scan.Id,
        TrackingNumber = scan.TrackingNumber,
        ScanType = scan.ScanType.ToString(),
        CourierId = scan.CourierId,
        Location = scan.Location,
        ScannedAtUtc = scan.ScannedAtUtc,
        Notes = scan.Notes
    };

    private static DomainScanType MapScanType(ScanType dtoType)
    {
        return dtoType switch
        {
            ScanType.Pickup => DomainScanType.Pickup,
            ScanType.InTransit => DomainScanType.InTransit,
            ScanType.Delivered => DomainScanType.Delivered,
            ScanType.Failed => DomainScanType.Failed,
            _ => throw new ArgumentOutOfRangeException(nameof(dtoType), $"Unexpected scan type: {dtoType}")
        };
    }
}
