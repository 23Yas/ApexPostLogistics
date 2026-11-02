namespace ApexPost.Infrastructure;

using ApexPost.Core.Application.Services;
using ApexPost.Core.Domain;

public sealed class SimulatedNotificationSink : INotificationSink
{
    private readonly List<DomainEvent> _events = [];

    public Task PublishAsync(DomainEvent domainEvent, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _events.Add(domainEvent);
        return Task.CompletedTask;
    }

    public IReadOnlyCollection<DomainEvent> Events => _events;
}

public sealed class CourierRateProvider : IShippingRateProvider
{
    private static readonly SemaphoreSlim RateLock = new(1, 1);
    private readonly Dictionary<string, decimal> _zoneRates = new(StringComparer.OrdinalIgnoreCase)
    {
        ["North"] = 1.20m,
        ["South"] = 1.10m,
        ["East"] = 1.35m,
        ["West"] = 1.25m,
        ["Central"] = 1.15m,
        ["Midlands"] = 1.18m,
        ["Scotland"] = 1.42m,
        ["Wales"] = 1.28m,
        ["Northern Ireland"] = 1.48m,
        ["Channel Islands"] = 1.55m
    };

    public async Task<decimal> GetRateAsync(string zone, ServiceLevel serviceLevel, bool isLetter, CancellationToken cancellationToken = default)
    {
        await RateLock.WaitAsync(cancellationToken);
        try
        {
            var multiplier = serviceLevel switch
            {
                ServiceLevel.Standard => 1m,
                ServiceLevel.Express => 1.4m,
                ServiceLevel.Registered => 1.8m,
                _ => 1m
            };

            var baseRate = _zoneRates.TryGetValue(zone, out var rate) ? rate : 1.20m;
            return isLetter ? baseRate * multiplier * 0.8m : baseRate * multiplier;
        }
        finally
        {
            RateLock.Release();
        }
    }
}