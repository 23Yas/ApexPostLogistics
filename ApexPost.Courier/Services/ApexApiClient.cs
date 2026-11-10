using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Maui.Storage;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ApexPost.Courier.Services;

/// <summary>Source-generated JSON context matching the v2 API contract (camelCase, enums as strings).</summary>
public static class ApiJson
{
    public static readonly ApiJsonContext Context = new ApiJsonContext();
}

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase, Converters = [typeof(JsonStringEnumConverter)])]
[JsonSerializable(typeof(ShipmentResponseV2))]
[JsonSerializable(typeof(CreateShipmentRequest))]
[JsonSerializable(typeof(ScanUploadRequest))]
[JsonSerializable(typeof(ScanUploadResponse))]
[JsonSerializable(typeof(ScanRejectionDto))]
[JsonSerializable(typeof(ShipmentScanDto))]
[JsonSerializable(typeof(ProblemDetailsDto))]
public partial class ApiJsonContext : JsonSerializerContext { }

public sealed class ShipmentResponseV2
{
    public Guid Id { get; set; }
    public string TrackingNumber { get; set; } = "";
    public decimal WeightKg { get; set; }
    public decimal Price { get; set; }
    public string Status { get; set; } = "";
    public RecipientDto? Recipient { get; set; }
    public LatestScanDto? LatestScan { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
}

public sealed class RecipientDto
{
    public string Name { get; set; } = "";
    public string Phone { get; set; } = "";
    public string Address { get; set; } = "";
    public string City { get; set; } = "";
}

public sealed class LatestScanDto
{
    public string ScanType { get; set; } = "";
    public string Location { get; set; } = "";
    public DateTimeOffset ScannedAtUtc { get; set; }
}

public sealed class CreateShipmentRequest
{
    public string TrackingNumber { get; set; } = "";
    public decimal WeightKg { get; set; }
    public string RecipientName { get; set; } = "";
    public string RecipientPhone { get; set; } = "";
    public string RecipientAddress { get; set; } = "";
    public string RecipientCity { get; set; } = "";
    public decimal Price { get; set; }
}

public sealed class ScanUploadRequest
{
    public List<ShipmentScanDto> Scans { get; set; } = new();
}

public sealed class ShipmentScanDto
{
    public Guid Id { get; set; }
    public string TrackingNumber { get; set; } = "";
    public ScanType ScanType { get; set; }
    public string CourierId { get; set; } = "";
    public string Location { get; set; } = "";
    public DateTimeOffset ScannedAtUtc { get; set; }
    public string Notes { get; set; } = "";
}

public enum ScanType { Pickup, InTransit, Delivered, Failed }

public sealed class ScanUploadResponse
{
    public int Accepted { get; set; }
    public int DuplicatesSkipped { get; set; }
    public int Rejected { get; set; }
    public List<ScanRejectionDto>? Rejections { get; set; }
}

public sealed class ScanRejectionDto
{
    public Guid ScanId { get; set; }
    public string Reason { get; set; } = string.Empty;
}

public sealed class ProblemDetailsDto
{
    public string? Title { get; set; }
    public int? Status { get; set; }
    public string? Detail { get; set; }
}

/// <summary>Attaches the bearer token from SecureStorage to every request.</summary>
public sealed class AuthHeaderHandler : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var token = await SecureStorage.GetAsync("auth_token");
        if (!string.IsNullOrEmpty(token))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }
        return await base.SendAsync(request, cancellationToken);
    }
}

/// <summary>Typed API client for the v2 ApexPost API.</summary>
public sealed class ApexApiClient
{
    /// <summary>Raised when the API returns 401 so the app can return to Login.</summary>
    public event Func<Task>? Unauthorized;

    private readonly HttpClient _http;

    public ApexApiClient(HttpClient http)
    {
        _http = http;
    }

    public async Task<ShipmentResponseV2?> GetShipmentAsync(string trackingNumber, CancellationToken ct = default)
    {
        var response = await _http.GetAsync($"api/v2/shipments/{trackingNumber}", ct);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound) return null;
        response.EnsureSuccessStatusCode();
        return JsonSerializer.Deserialize<ShipmentResponseV2>(await response.Content.ReadAsStringAsync(ct), ApiJson.Context.ShipmentResponseV2);
    }

    public async Task<ShipmentResponseV2> CreateShipmentAsync(CreateShipmentRequest request, CancellationToken ct = default)
    {
        var response = await _http.PostAsJsonAsync("api/v2/shipments", request, ApiJson.Context.CreateShipmentRequest, ct);
        await EnsureSuccessWithProblemDetails(response, ct);
        return JsonSerializer.Deserialize<ShipmentResponseV2>(await response.Content.ReadAsStringAsync(ct), ApiJson.Context.ShipmentResponseV2)!;
    }

    public async Task<ScanUploadResponse> UploadScansAsync(string trackingNumber, List<ShipmentScanDto> scans, CancellationToken ct = default)
    {
        var response = await _http.PostAsJsonAsync($"api/v2/shipments/{trackingNumber}/scans",
            new ScanUploadRequest { Scans = scans }, ApiJson.Context.ScanUploadRequest, ct);
        await EnsureSuccessWithProblemDetails(response, ct);
        return JsonSerializer.Deserialize<ScanUploadResponse>(await response.Content.ReadAsStringAsync(ct), ApiJson.Context.ScanUploadResponse)!;
    }

    /// <summary>Reads ProblemDetails on error responses and throws a friendly exception.</summary>
    private async Task EnsureSuccessWithProblemDetails(HttpResponseMessage response, CancellationToken ct)
    {
        if (response.IsSuccessStatusCode) return;
        if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
        {
            // Expired/invalid token: clear it and notify the app to return to Login.
            SecureStorage.Remove("auth_token");
            if (Unauthorized is not null)
            {
                await Unauthorized.Invoke();
            }
        }
        string? detail = null;
        try
        {
            var problem = JsonSerializer.Deserialize<ProblemDetailsDto>(await response.Content.ReadAsStringAsync(ct), ApiJson.Context.ProblemDetailsDto);
            detail = problem?.Detail ?? problem?.Title;
        }
        catch { /* malformed body - fall through */ }
        throw new HttpRequestException($"API error {(int)response.StatusCode}: {detail ?? response.ReasonPhrase}", null, response.StatusCode);
    }
}
