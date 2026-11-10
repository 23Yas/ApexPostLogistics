using ApexPost.Courier.Offline;
using Microsoft.Extensions.Logging;

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
					ScanType = Enum.Parse<ScanType>(scan.ScanType.ToString()),
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
			return new UploadResult { Success = false, ErrorMessage = ex.Message };
		}
	}
}
