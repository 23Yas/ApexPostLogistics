using ApexPost.Core.Application.Interfaces;
using ApexPost.Core.Domain;
using Microsoft.EntityFrameworkCore;

namespace ApexPost.Infrastructure;

public sealed class ShipmentRepository : IShipmentRepository
{
    private readonly ApexPostDbContext _context;

    public ShipmentRepository(ApexPostDbContext context)
    {
        _context = context;
    }

    public async Task<Shipment?> GetByTrackingNumberAsync(string trackingNumber, CancellationToken cancellationToken = default)
    {
        return await _context.Shipments
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.TrackingReference == trackingNumber, cancellationToken);
    }

    public async Task<IReadOnlyCollection<Shipment>> GetPagedAsync(int page, int pageSize, string? status = null, string? city = null, CancellationToken cancellationToken = default)
    {
        var query = _context.Shipments.AsNoTracking();

        if (!string.IsNullOrEmpty(status))
        {
            query = query.Where(s => s.Status == status);
        }

        // Note: City filtering would require joins with other tables if shipment has city info
        // For now, we'll just implement page/size and status filtering

        return await query
            .OrderBy(s => s.CreatedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }

    public async Task<Shipment> CreateAsync(Shipment shipment, CancellationToken cancellationToken = default)
    {
        _context.Shipments.Add(shipment);
        await _context.SaveChangesAsync(cancellationToken);
        return shipment;
    }

    public async Task<Shipment?> UpdateAsync(string trackingNumber, Shipment shipment, CancellationToken cancellationToken = default)
    {
        var existing = await _context.Shipments
            .FirstOrDefaultAsync(s => s.TrackingReference == trackingNumber, cancellationToken);

        if (existing == null)
        {
            return null;
        }

        // Update editable fields
        existing.WeightKg = shipment.WeightKg;
        existing.Price = shipment.Price;
        // Status is updated via separate method

        await _context.SaveChangesAsync(cancellationToken);
        return existing;
    }

    public async Task<bool> UpdateStatusAsync(string trackingNumber, string newStatus, CancellationToken cancellationToken = default)
    {
        var shipment = await _context.Shipments
            .FirstOrDefaultAsync(s => s.TrackingReference == trackingNumber, cancellationToken);

        if (shipment == null)
        {
            return false;
        }

        // Validate status transition
        if (!IsValidStatusTransition(shipment.Status, newStatus))
        {
            return false;
        }

        shipment.Status = newStatus;
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> DeleteAsync(string trackingNumber, CancellationToken cancellationToken = default)
    {
        var shipment = await _context.Shipments
            .FirstOrDefaultAsync(s => s.TrackingReference == trackingNumber, cancellationToken);

        if (shipment == null)
        {
            return false;
        }

        // Only allow deletion if status is Created
        if (shipment.Status != "Created")
        {
            return false;
        }

        _context.Shipments.Remove(shipment);
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    private static bool IsValidStatusTransition(string currentStatus, string newStatus)
    {
        var allowedTransitions = new Dictionary<string, string[]>
        {
            ["Created"] = new[] { "PickedUp" },
            ["PickedUp"] = new[] { "InTransit" },
            ["InTransit"] = new[] { "OutForDelivery" },
            ["OutForDelivery"] = new[] { "Delivered", "Failed" },
            ["Delivered"] = Array.Empty<string>(),
            ["Failed"] = Array.Empty<string>()
        };

        if (!allowedTransitions.TryGetValue(currentStatus, out var allowed))
        {
            return false;
        }

        return allowed.Contains(newStatus);
    }
}
