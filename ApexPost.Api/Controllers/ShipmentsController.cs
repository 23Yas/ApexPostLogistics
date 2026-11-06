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
[Route("api/[controller]")]
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
    [ProducesResponseType(typeof(IEnumerable<Shipment>), StatusCodes.Status200OK)]
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
        return Ok(shipments);
    }

    /// <summary>
    /// Get a single shipment by tracking number
    /// </summary>
    /// <param name="trackingNumber">The tracking number of the shipment</param>
    /// <returns>Shipment details</returns>
    [HttpGet("{trackingNumber}", Name = "GetShipment")]
    [Tags("Shipments")]
    [EndpointSummary("Get shipment by tracking number")]
    [ProducesResponseType(typeof(Shipment), StatusCodes.Status200OK)]
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

        return Ok(shipment);
    }

    /// <summary>
    /// Create a new shipment
    /// </summary>
    /// <param name="request">Shipment creation request</param>
    /// <returns>Created shipment with Location header</returns>
    [HttpPost(Name = "CreateShipment")]
    [Tags("Shipments")]
    [EndpointSummary("Create a new shipment")]
    [ProducesResponseType(typeof(Shipment), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateShipment([FromBody] CreateShipmentRequest request)
    {
        // Check if tracking number already exists
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
        return CreatedAtAction(nameof(GetShipment), new { trackingNumber = created.TrackingReference }, created);
    }

    /// <summary>
    /// Update editable shipment details
    /// </summary>
    /// <param name="trackingNumber">The tracking number of the shipment</param>
    /// <param name="request">Shipment update request</param>
    /// <returns>Updated shipment</returns>
    [HttpPut("{trackingNumber}", Name = "UpdateShipment")]
    [Tags("Shipments")]
    [EndpointSummary("Update shipment details")]
    [ProducesResponseType(typeof(Shipment), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateShipment(string trackingNumber, [FromBody] UpdateShipmentRequest request)
    {
        if (request.TrackingNumber != trackingNumber)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Tracking number mismatch",
                Detail = "Tracking number in URL must match request body",
                Status = StatusCodes.Status400BadRequest
            });
        }

        var shipment = new Shipment
        {
            TrackingReference = request.TrackingNumber,
            WeightKg = request.WeightKg,
            Price = request.Price
        };

        var updated = await _shipmentService.UpdateAsync(trackingNumber, shipment);
        if (updated == null)
        {
            return NotFound(new ProblemDetails
            {
                Title = "Shipment not found",
                Detail = $"No shipment found with tracking number {trackingNumber}",
                Status = StatusCodes.Status404NotFound
            });
        }

        return Ok(updated);
    }

    /// <summary>
    /// Update shipment status only
    /// </summary>
    /// <param name="trackingNumber">The tracking number of the shipment</param>
    /// <param name="request">Status update request</param>
    /// <returns>Success or conflict response</returns>
    [HttpPatch("{trackingNumber}/status", Name = "UpdateShipmentStatus")]
    [Authorize(Roles = "Courier")]
    [Tags("Shipments")]
    [EndpointSummary("Update shipment status")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UpdateStatus(string trackingNumber, [FromBody] UpdateStatusRequest request)
    {
        var success = await _shipmentService.UpdateStatusAsync(trackingNumber, request.Status);
        if (!success)
        {
            var existing = await _shipmentService.GetByTrackingNumberAsync(trackingNumber);
            if (existing == null)
            {
                return NotFound(new ProblemDetails
                {
                    Title = "Shipment not found",
                    Detail = $"No shipment found with tracking number {trackingNumber}",
                    Status = StatusCodes.Status404NotFound
                });
            }

            return Conflict(new ProblemDetails
            {
                Title = "Invalid status transition",
                Detail = $"Cannot transition from {existing.Status} to {request.Status}",
                Status = StatusCodes.Status409Conflict
            });
        }

        return Ok();
    }

    /// <summary>
    /// Delete a shipment (only allowed when status is Created)
    /// </summary>
    /// <param name="trackingNumber">The tracking number of the shipment</param>
    /// <returns>No content on success</returns>
    [HttpDelete("{trackingNumber}", Name = "DeleteShipment")]
    [Authorize(Roles = "Admin")]
    [Tags("Shipments")]
    [EndpointSummary("Delete a shipment")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> DeleteShipment(string trackingNumber)
    {
        var success = await _shipmentService.DeleteAsync(trackingNumber);
        if (!success)
        {
            var existing = await _shipmentService.GetByTrackingNumberAsync(trackingNumber);
            if (existing == null)
            {
                return NotFound(new ProblemDetails
                {
                    Title = "Shipment not found",
                    Detail = $"No shipment found with tracking number {trackingNumber}",
                    Status = StatusCodes.Status404NotFound
                });
            }

            return Conflict(new ProblemDetails
            {
                Title = "Cannot delete shipment",
                Detail = $"Shipment with status {existing.Status} cannot be deleted. Only shipments with status 'Created' can be deleted.",
                Status = StatusCodes.Status409Conflict
            });
        }

        return NoContent();
    }

    /// <summary>
    /// Bulk upload of offline scans
    /// </summary>
    /// <param name="trackingNumber">The tracking number of the shipment</param>
    /// <param name="request">Scan upload request with list of scans</param>
    /// <returns>Upload summary with accepted, duplicates skipped, and rejected counts</returns>
    [HttpPost("{trackingNumber}/scans", Name = "UploadScans")]
    [Authorize(Roles = "Courier")]
    [Tags("Scans")]
    [EndpointSummary("Bulk upload shipment scans")]
    [ProducesResponseType(typeof(ScanUploadResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UploadScans(string trackingNumber, [FromBody] ScanUploadRequest request)
    {
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

        var (accepted, duplicatesSkipped, rejected) = await _shipmentService.UploadScansAsync(trackingNumber, scans);

        var response = new ScanUploadResponse
        {
            Accepted = accepted,
            DuplicatesSkipped = duplicatesSkipped,
            Rejected = rejected
        };

        return Ok(response);
    }

    /// <summary>
    /// Get scan history for a shipment
    /// </summary>
    /// <param name="trackingNumber">The tracking number of the shipment</param>
    /// <returns>List of scans for the shipment</returns>
    [HttpGet("{trackingNumber}/scans", Name = "GetScans")]
    [Tags("Scans")]
    [EndpointSummary("Get shipment scan history")]
    [ProducesResponseType(typeof(IEnumerable<ShipmentScan>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetScans(string trackingNumber)
    {
        // For now, return empty list since we don't have scan persistence in SQL yet
        // This will be implemented when we add scan storage
        return Ok(Array.Empty<ShipmentScan>());
    }

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

public sealed class ScanUploadResponse
{
    public int Accepted { get; init; }
    public int DuplicatesSkipped { get; init; }
    public int Rejected { get; init; }
}
