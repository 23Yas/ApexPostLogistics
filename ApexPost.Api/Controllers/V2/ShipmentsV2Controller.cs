using Asp.Versioning;
using ApexPost.Api.DTOs;
using ApexPost.Api.DTOs.V2;
using ApexPost.Core.Application.Services;
using ApexPost.Core.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ScanType = ApexPost.Api.DTOs.ScanType;
using DomainScanType = ApexPost.Core.Domain.ScanType;

namespace ApexPost.Api.Controllers.V2;

/// <summary>
/// Shipments API v2 controller with enhanced response structure
/// </summary>
[ApiController]
[ApiVersion(2.0)]
[Route("api/v2/shipments")]
[Authorize]
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
        var v2Responses = new List<ShipmentResponseV2>();
        foreach (var s in shipments)
        {
            v2Responses.Add(await MapToV2ResponseAsync(s));
        }
        return Ok(v2Responses);
    }

    /// <summary>
    /// Get a single shipment by tracking number (v2)
    /// </summary>
    [HttpGet("{trackingNumber:regex(^APX-\\d{{8}}$)}", Name = "GetShipmentV2")]
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

        return Ok(await MapToV2ResponseAsync(shipment));
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
            RecipientName = request.RecipientName,
            RecipientPhone = request.RecipientPhone,
            RecipientAddress = request.RecipientAddress,
            RecipientCity = request.RecipientCity,
            CreatedAtUtc = DateTimeOffset.UtcNow
        };

        var created = await _shipmentService.CreateAsync(shipment);
        return CreatedAtAction(nameof(GetShipment), new { trackingNumber = created.TrackingReference }, await MapToV2ResponseAsync(created));
    }

    /// <summary>
    /// Replace editable shipment details (v2)
    /// </summary>
    [HttpPut("{trackingNumber:regex(^APX-\\d{{8}}$)}", Name = "UpdateShipmentV2")]
    [Tags("Shipments")]
    [EndpointSummary("Update shipment details (v2)")]
    [ProducesResponseType(typeof(ShipmentResponseV2), StatusCodes.Status200OK)]
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
            Price = request.Price,
            RecipientName = request.RecipientName ?? string.Empty,
            RecipientPhone = request.RecipientPhone ?? string.Empty,
            RecipientAddress = request.RecipientAddress ?? string.Empty,
            RecipientCity = request.RecipientCity ?? string.Empty
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

        return Ok(await MapToV2ResponseAsync(updated));
    }

    /// <summary>
    /// Delete a shipment - only while status is Created (v2, Admin only)
    /// </summary>
    [HttpDelete("{trackingNumber:regex(^APX-\\d{{8}}$)}", Name = "DeleteShipmentV2")]
    [Authorize(Roles = "Admin")]
    [Tags("Shipments")]
    [EndpointSummary("Delete a shipment (v2)")]
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
    /// Update shipment status only (v2)
    /// </summary>
    [HttpPatch("{trackingNumber:regex(^APX-\\d{{8}}$)}/status", Name = "UpdateShipmentStatusV2")]
    [Authorize(Roles = "Courier")]
    [Tags("Shipments")]
    [EndpointSummary("Update shipment status (v2)")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UpdateStatus(string trackingNumber, [FromBody] UpdateStatusRequest request)
    {
        if (!ShipmentStatusRules.IsKnownStatus(request.Status))
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Unknown status",
                Detail = $"'{request.Status}' is not a valid shipment status",
                Status = StatusCodes.Status400BadRequest
            });
        }

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
    /// Bulk upload of offline scans (v2)
    /// </summary>
    [HttpPost("{trackingNumber:regex(^APX-\\d{{8}}$)}/scans", Name = "UploadScansV2")]
    [Authorize(Roles = "Courier")]
    [Tags("Scans")]
    [EndpointSummary("Bulk upload shipment scans (v2)")]
    [ProducesResponseType(typeof(ScanUploadResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UploadScans(string trackingNumber, [FromBody] ScanUploadRequest request)
    {
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

        return Ok(new ScanUploadResponse
        {
            Accepted = outcome.Accepted,
            DuplicatesSkipped = outcome.DuplicatesSkipped,
            Rejected = outcome.Rejected,
            Rejections = outcome.Rejections
                .Select(r => new ScanRejectionDto { ScanId = r.ScanId, Reason = r.Reason })
                .ToList()
        });
    }

    /// <summary>
    /// Get scan history for a shipment (v2)
    /// </summary>
    [HttpGet("{trackingNumber:regex(^APX-\\d{{8}}$)}/scans", Name = "GetScansV2")]
    [Tags("Scans")]
    [EndpointSummary("Get shipment scan history (v2)")]
    [ProducesResponseType(typeof(IEnumerable<ShipmentScan>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetScans(string trackingNumber)
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

        var scans = await _shipmentService.GetScansAsync(trackingNumber);
        return Ok(scans);
    }

    private async Task<ShipmentResponseV2> MapToV2ResponseAsync(Shipment shipment)
    {
        var latestScan = await _shipmentService.GetLatestScanAsync(shipment.TrackingReference);
        return new ShipmentResponseV2
        {
            Id = shipment.Id,
            TrackingNumber = shipment.TrackingReference,
            WeightKg = shipment.WeightKg,
            Price = shipment.Price,
            Status = shipment.Status,
            Recipient = new RecipientDto
            {
                Name = shipment.RecipientName,
                Phone = shipment.RecipientPhone,
                Address = shipment.RecipientAddress,
                City = shipment.RecipientCity
            },
            LatestScan = latestScan is null ? null : new LatestScanDto
            {
                ScanType = latestScan.ScanType.ToString(),
                Location = latestScan.Location,
                ScannedAtUtc = latestScan.ScannedAtUtc
            },
            CreatedAtUtc = shipment.CreatedAtUtc
        };
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
