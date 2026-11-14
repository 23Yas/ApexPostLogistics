using ApexPost.ApiClient;
using ApexPost.Courier.Offline;
using Microsoft.Extensions.Logging;
using System.Net;
using ClientScanType = ApexPost.ApiClient.ScanType;

namespace ApexPost.Courier.Services;

/// <summary>Real IShipmentUploader that posts scans to the v2 API (replaces the Part A fake).</summary>
public sealed class ApiShipmentUploader : IShipmentUploader
{
	private readonly ApexApiClient _apiClient;
	private readonly ILogger<ApiShipmentUploader> _logger;

	public ApiShipmentUploader(ApexApiClient apiClient, ILogger<ApiShipmentUploader> logger)
	{
		_apiClient = apiClient;
		_logger = logger;
	}

	public async Task<UploadResult> UploadScanAsync(ShipmentScan scan, CancellationToken cancellationToken = default)
	{
		try
		{
			var result = await _apiClient.UploadScansAsync(scan.TrackingNumber, new List<ShipmentScanDto>
			{
				new()
				{
					Id = scan.Id,
					TrackingNumber = scan.TrackingNumber,
					ScanType = Enum.Parse<ClientScanType>(scan.ScanType.ToString()),
					CourierId = scan.CourierId,
					Location = scan.Location,
					ScannedAtUtc = scan.ScannedAtUtc,
					Notes = scan.Notes
				}
			}, cancellationToken);

			return result.Accepted > 0 || (result.DuplicatesSkipped > 0 && result.Rejected == 0)
				? new UploadResult { Success = true }
				: new UploadResult { Success = false, ErrorMessage = $"Rejected: {result.Rejected}" };
		}
		catch (Exception ex)
		{
			_logger.LogWarning(ex, "Scan upload failed for {ScanId}", scan.Id);
			return new UploadResult { Success = false, ErrorMessage = ex.Message, IsTransient = IsTransient(ex) };
		}
	}

	/// <summary>
	/// Transport failures, timeouts, 5xx and 401 (expired token - retried after re-login)
	/// are transient: the scan stays Pending. Other 4xx responses are real rejections.
	/// </summary>
	internal static bool IsTransient(Exception ex) => ex switch
	{
		HttpRequestException { StatusCode: { } status } =>
			(int)status >= 500
			|| status is HttpStatusCode.RequestTimeout
				or HttpStatusCode.TooManyRequests
				or HttpStatusCode.Unauthorized,
		HttpRequestException => true, // no status code: DNS/socket/connection failure
		TaskCanceledException or OperationCanceledException => true, // timeout or caller cancel
		_ => false
	};
}
