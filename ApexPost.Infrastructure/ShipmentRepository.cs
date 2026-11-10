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

        if (!string.IsNullOrEmpty(city))
        {
            query = query.Where(s => s.RecipientCity == city);
        }

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
        if (!string.IsNullOrEmpty(shipment.RecipientName)) existing.RecipientName = shipment.RecipientName;
        if (!string.IsNullOrEmpty(shipment.RecipientPhone)) existing.RecipientPhone = shipment.RecipientPhone;
        if (!string.IsNullOrEmpty(shipment.RecipientAddress)) existing.RecipientAddress = shipment.RecipientAddress;
        if (!string.IsNullOrEmpty(shipment.RecipientCity)) existing.RecipientCity = shipment.RecipientCity;
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

        _context.Shipments.Remove(shipment);
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<ShipmentScan?> GetScanByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.ShipmentScans
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
    }

    public async Task AddScanAsync(ShipmentScan scan, CancellationToken cancellationToken = default)
    {
        _context.ShipmentScans.Add(scan);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<ShipmentScan>> GetScansByTrackingNumberAsync(string trackingNumber, CancellationToken cancellationToken = default)
    {
        return await _context.ShipmentScans
            .AsNoTracking()
            .Where(s => s.TrackingNumber == trackingNumber)
            .OrderBy(s => s.ScannedAtUtc)
            .ToListAsync(cancellationToken);
    }

    public async Task<ShipmentScan?> GetLatestScanAsync(string trackingNumber, CancellationToken cancellationToken = default)
    {
        return await _context.ShipmentScans
            .AsNoTracking()
            .Where(s => s.TrackingNumber == trackingNumber)
            .OrderByDescending(s => s.ScannedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);
    }
}
