namespace ApexPost.Tests;

using ApexPost.Core.Application.Services;
using ApexPost.Core.Domain;

internal sealed class RecordingNotificationSink : INotificationSink
{
    public List<DomainEvent> Events { get; } = new();

    public Task PublishAsync(DomainEvent domainEvent, CancellationToken cancellationToken = default)
    {
        Events.Add(domainEvent);
        return Task.CompletedTask;
    }
}
