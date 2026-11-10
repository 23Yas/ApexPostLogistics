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
	private readonly ILogger<OfflineSyncService> _logger;
	private bool _isSyncing;

	public DateTimeOffset? LastSyncTimeUtc { get; private set; }
	public int LastFailureCount { get; private set; }
	public string CircuitState { get; private set; } = "Closed";

	public event Action<int>? PendingCountChanged;

	public OfflineSyncService(
		ApexApiClient apiClient,
		IScanRepository scanRepository,
		ILogger<OfflineSyncService> logger)
	{
		_apiClient = apiClient;
		_scanRepository = scanRepository;
		_logger = logger;
	}

	/// <summary>Subscribe to MAUI connectivity changes; sync automatically on reconnect.</summary>
	public void StartConnectivityListener()
	{
		Connectivity.ConnectivityChanged += OnConnectivityChanged;
	}

	private void OnConnectivityChanged(object? sender, ConnectivityChangedEventArgs e)
	{
		if (e.NetworkAccess == NetworkAccess.Internet)
		{
			_ = SyncAsync();
		}
	}

	/// <summary>Upload pending scans in batches of 20, marking each Synced or Failed.</summary>
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
					var scans = group.ToList();
					try
					{
						var dtos = scans.Select(s => new ShipmentScanDto
						{
							Id = s.Id,
							TrackingNumber = s.TrackingNumber,
							ScanType = Enum.Parse<ScanType>(s.ScanType.ToString()),
							CourierId = s.CourierId,
							Location = s.Location,
							ScannedAtUtc = s.ScannedAtUtc,
							Notes = s.Notes
						}).ToList();

						var result = await _apiClient.UploadScansAsync(group.Key, dtos, ct);
						summary.Synced += result.Accepted;
						summary.DuplicatesSkipped += result.DuplicatesSkipped;
						summary.Failed += result.Rejected;
						LastFailureCount = result.Rejected;
						CircuitState = "Closed";

						// Mark each scan based on the per-scan outcome.
						var rejectedIds = result.Rejections?.Select(r => r.ScanId).ToHashSet() ?? new HashSet<Guid>();
						foreach (var scan in scans)
						{
							scan.SyncStatus = rejectedIds.Contains(scan.Id) ? SyncStatus.Failed : SyncStatus.Synced;
							await _scanRepository.UpdateAsync(scan, ct);
						}
					}
					catch (Exception ex)
					{
						_logger.LogWarning(ex, "Batch upload failed for {Tracking}", group.Key);
						CircuitState = "Open";
						summary.Failed += scans.Count;
						LastFailureCount += scans.Count;
						// Leave scans Pending so they retry on the next connectivity event.
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
