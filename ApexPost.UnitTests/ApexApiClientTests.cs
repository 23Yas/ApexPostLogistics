namespace ApexPost.Tests;

using System.Net;
using System.Text;
using ApexPost.ApiClient;

/// <summary>
/// Unit tests for the typed API client using a fake HttpMessageHandler.
/// No real network calls: every test scripts the handler's response.
/// AI-assisted draft (see COPILOT-LOG.md) - reviewed and kept.
/// </summary>
public sealed class ApexApiClientTests
{
    private static ApexApiClient ClientWith(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> responder)
    {
        var handler = new FakeHandler(responder);
        return new ApexApiClient(new HttpClient(handler) { BaseAddress = new Uri("https://api.test/") });
    }

    private static HttpResponseMessage Json(HttpStatusCode status, string body) => new(status)
    {
        Content = new StringContent(body, Encoding.UTF8, "application/json")
    };

    [Fact]
    public async Task GetShipmentAsync_200_ReturnsShipment()
    {
        var client = ClientWith((_, _) => Task.FromResult(Json(HttpStatusCode.OK,
            """{"id":"11111111-1111-1111-1111-111111111111","trackingNumber":"APX-12345678","weightKg":1.5,"price":9.99,"status":"Created","recipient":null,"latestScan":null,"createdAtUtc":"2026-01-01T00:00:00Z"}""")));

        var shipment = await client.GetShipmentAsync("APX-12345678");

        Assert.NotNull(shipment);
        Assert.Equal("APX-12345678", shipment!.TrackingNumber);
        Assert.Equal("Created", shipment.Status);
    }

    [Fact]
    public async Task GetShipmentAsync_404_ReturnsNull()
    {
        var client = ClientWith((_, _) => Task.FromResult(Json(HttpStatusCode.NotFound, "{}")));

        Assert.Null(await client.GetShipmentAsync("APX-00000000"));
    }

    [Fact]
    public async Task GetShipmentAsync_500_ThrowsHttpRequestException()
    {
        var client = ClientWith((_, _) => Task.FromResult(Json(HttpStatusCode.InternalServerError, "{}")));

        await Assert.ThrowsAsync<HttpRequestException>(() => client.GetShipmentAsync("APX-12345678"));
    }

    [Fact]
    public async Task CreateShipmentAsync_ProblemDetails_ThrowsWithDetail()
    {
        var client = ClientWith((_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.BadRequest)
        {
            Content = new StringContent(
                """{"title":"Bad Request","status":400,"detail":"weightKg must be at most 70"}""",
                Encoding.UTF8, "application/problem+json")
        }));

        var ex = await Assert.ThrowsAsync<HttpRequestException>(() =>
            client.CreateShipmentAsync(new CreateShipmentRequest { TrackingNumber = "APX-1" }));

        Assert.Contains("weightKg must be at most 70", ex.Message);
    }

    [Fact]
    public async Task UploadScansAsync_Cancellation_ThrowsTaskCanceled()
    {
        var client = ClientWith((_, ct) => Task.FromException<HttpResponseMessage>(
            new TaskCanceledException("simulated timeout", null, ct)));

        await Assert.ThrowsAsync<TaskCanceledException>(() =>
            client.UploadScansAsync("APX-12345678", new List<ShipmentScanDto>()));
    }

    [Fact]
    public async Task CreateShipmentAsync_401_RaisesUnauthorizedEvent()
    {
        var unauthorizedRaised = false;
        var client = ClientWith((_, _) => Task.FromResult(Json(HttpStatusCode.Unauthorized, "{}")));
        client.Unauthorized += () => { unauthorizedRaised = true; return Task.CompletedTask; };

        await Assert.ThrowsAsync<HttpRequestException>(() =>
            client.CreateShipmentAsync(new CreateShipmentRequest { TrackingNumber = "APX-1" }));

        Assert.True(unauthorizedRaised);
    }

    [Fact]
    public async Task UploadScansAsync_200_ReturnsCounts()
    {
        var client = ClientWith((_, _) => Task.FromResult(Json(HttpStatusCode.OK,
            """{"accepted":2,"duplicatesSkipped":1,"rejected":0,"rejections":[]}""")));

        var result = await client.UploadScansAsync("APX-12345678", new List<ShipmentScanDto>
        {
            new() { TrackingNumber = "APX-12345678", ScanType = ScanType.Pickup, CourierId = "c1" }
        });

        Assert.Equal(2, result.Accepted);
        Assert.Equal(1, result.DuplicatesSkipped);
    }

    private sealed class FakeHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> _responder;

        public FakeHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> responder)
            => _responder = responder;

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => _responder(request, cancellationToken);
    }
}
