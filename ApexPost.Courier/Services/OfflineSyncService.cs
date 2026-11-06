using ApexPost.Courier.Offline;
using Microsoft.Extensions.Logging;

namespace ApexPost.Courier.Services;

/// <summary>
/// Uploads pending offline scans to the v2 API when connectivity returns.
/// </summary>
public sealed class OfflineSyncService
{
	private readonly ApexApiClient _apiClient;
	private readonly IScanRepository _scanRepository;
	private readonly OutboxService _outboxService;
	private readonly ILogger<OfflineSyncService> _logger;
	private bool _isSyncing;

	public DateTimeOffset? LastSyncTimeUtc { get; private set; }
	public int LastFailureCount { get; private set; }
	public string CircuitState { get; private set; } = "Closed";

	public OfflineSyncService(
		ApexApiClient apiClient,
		IScanRepository scanRepository,
		OutboxService outboxService,
		ILogger<OfflineSyncService> logger)
	{
		_apiClient = apiClient;
		_scanRepository = scanRepository;
		_outboxService = outboxService;
		_logger = logger;
	}

	/// <summary>Upload pending scans in batches of 20.</summary>
	public async Task<SyncSummary> SyncAsync(CancellationToken ct = default)
	{
		if (_isSyncing) return new SyncSummary();
		_isSyncing = true;
		try
		{
			var pending = await _scanRepository.GetPendingScansAsync(ct);
			var summary = new SyncSummary { TotalPending = pending.Count };

			foreach (var batch in pending.Chunk(20))
			{
				var byTracking = batch.GroupBy(s => s.TrackingNumber);
				foreach (var group in byTracking)
				{
					try
					{
						var scans = group.Select(s => new ShipmentScanDto
						{
							Id = s.Id,
							TrackingNumber = s.TrackingNumber,
							ScanType = Enum.Parse<ScanType>(s.ScanType.ToString()),
							CourierId = s.CourierId,
							Location = s.Location,
							ScannedAtUtc = s.ScannedAtUtc,
							Notes = s.Notes
						}).ToList();

						var result = await _apiClient.UploadScansAsync(group.Key, scans, ct);
						summary.Synced += result.Accepted;
						summary.DuplicatesSkipped += result.DuplicatesSkipped;
						summary.Failed += result.Rejected;
						LastFailureCount = result.Rejected;
					}
					catch (Exception ex)
					{
						_logger.LogWarning(ex, "Batch upload failed for {Tracking}", group.Key);
						summary.Failed += group.Count();
						LastFailureCount += group.Count();
					}
				}
			}

			LastSyncTimeUtc = DateTimeOffset.UtcNow;
			return summary;
		}
		finally
		{
			_isSyncing = false;
		}
	}
}

public sealed class SyncSummary
{
	public int TotalPending { get; set; }
	public int Synced { get; set; }
	public int DuplicatesSkipped { get; set; }
	public int Failed { get; set; }
}
