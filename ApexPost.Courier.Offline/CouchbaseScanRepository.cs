using Couchbase.Lite;
using Couchbase.Lite.Query;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace ApexPost.Courier.Offline;

public sealed class CouchbaseScanRepository : IScanRepository
{
    private readonly CourierDatabase _database;
    private readonly ILogger<CouchbaseScanRepository> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    public CouchbaseScanRepository(CourierDatabase database, ILogger<CouchbaseScanRepository> logger)
    {
        _database = database;
        _logger = logger;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };
    }

    public Task<ShipmentScan?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return Task.Run(() => GetById(id), cancellationToken);
    }

    private ShipmentScan? GetById(Guid id)
    {
        var docId = $"scan::{id}";
        var collection = _database.DefaultCollection;

        try
        {
            var document = collection.GetDocument(docId);
            return DeserializeScan(document);
        }
        catch (CouchbaseLiteException)
        {
            _logger?.LogDebug("Scan with ID {ScanId} not found", id);
            return null;
        }
    }

    public Task CreateAsync(ShipmentScan scan, CancellationToken cancellationToken = default)
    {
        return Task.Run(() => Create(scan), cancellationToken);
    }

    private void Create(ShipmentScan scan)
    {
        var docId = $"scan::{scan.Id}";
        var collection = _database.DefaultCollection;

        var mutableDoc = new MutableDocument(docId);
        mutableDoc.SetString("id", scan.Id.ToString());
        mutableDoc.SetString("trackingNumber", scan.TrackingNumber);
        mutableDoc.SetString("scanType", scan.ScanType.ToString());
        mutableDoc.SetString("courierId", scan.CourierId);
        mutableDoc.SetString("location", scan.Location);
        mutableDoc.SetDate("scannedAtUtc", scan.ScannedAtUtc.DateTime);
        mutableDoc.SetString("notes", scan.Notes);
        mutableDoc.SetString("syncStatus", scan.SyncStatus.ToString());

        collection.Save(mutableDoc);
        _logger?.LogInformation("Created scan {ScanId} for tracking {TrackingNumber}", scan.Id, scan.TrackingNumber);
    }

    public Task UpdateAsync(ShipmentScan scan, CancellationToken cancellationToken = default)
    {
        return Task.Run(() => Update(scan), cancellationToken);
    }

    private void Update(ShipmentScan scan)
    {
        var docId = $"scan::{scan.Id}";
        var collection = _database.DefaultCollection;

        try
        {
            var document = collection.GetDocument(docId);
            var mutableDoc = document.ToMutable();

            mutableDoc.SetString("id", scan.Id.ToString());
            mutableDoc.SetString("trackingNumber", scan.TrackingNumber);
            mutableDoc.SetString("scanType", scan.ScanType.ToString());
            mutableDoc.SetString("courierId", scan.CourierId);
            mutableDoc.SetString("location", scan.Location);
            mutableDoc.SetDate("scannedAtUtc", scan.ScannedAtUtc.DateTime);
            mutableDoc.SetString("notes", scan.Notes);
            mutableDoc.SetString("syncStatus", scan.SyncStatus.ToString());

            collection.Save(mutableDoc);
            _logger?.LogInformation("Updated scan {ScanId}", scan.Id);
        }
        catch (CouchbaseLiteException)
        {
            _logger?.LogError("Scan with ID {ScanId} not found for update", scan.Id);
            throw;
        }
    }

    public Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return Task.Run(() => Delete(id), cancellationToken);
    }

    private void Delete(Guid id)
    {
        var docId = $"scan::{id}";
        var collection = _database.DefaultCollection;

        var document = collection.GetDocument(docId);
        if (document is null)
        {
            _logger?.LogWarning("Scan {ScanId} not found for deletion", id);
            return;
        }

        collection.Delete(document);
        _logger?.LogInformation("Deleted scan {ScanId}", id);
    }

    public Task<List<ShipmentScan>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return Task.Run(GetAll, cancellationToken);
    }

    private List<ShipmentScan> GetAll()
    {
        var collection = _database.DefaultCollection;
        var scans = new List<ShipmentScan>();

        using var query = QueryBuilder.Select(
                SelectResult.All())
            .From(DataSource.Collection(collection));

        var resultSet = query.Execute();

        foreach (var result in resultSet.AllResults())
        {
            var dict = result.GetDictionary(0);

            if (dict == null)
                continue;

            var dictionary = new Dictionary<string, object>();

            foreach (var key in dict.Keys)
            {
                dictionary[key] = dict.GetValue(key);
            }

            var scan = DeserializeFromDictionary(dictionary);

            if (scan != null)
            {
                scans.Add(scan);
            }
        }

        return scans;
    }

    public Task<List<ShipmentScan>> GetByTrackingNumberAsync(string trackingNumber, CancellationToken cancellationToken = default)
    {
        return Task.Run(() => GetByTrackingNumber(trackingNumber), cancellationToken);
    }

    private List<ShipmentScan> GetByTrackingNumber(string trackingNumber)
    {
        var collection = _database.DefaultCollection;
        var scans = new List<ShipmentScan>();

        using var query = QueryBuilder.Select(
                SelectResult.All())
            .From(DataSource.Collection(collection))
            .Where(Expression.Property("trackingNumber")
                .EqualTo(Expression.String(trackingNumber)))
            .OrderBy(Ordering.Property("scannedAtUtc").Ascending());

        var resultSet = query.Execute();

        foreach (var result in resultSet.AllResults())
        {
            var dict = result.GetDictionary(0);

            if (dict == null)
                continue;

            var dictionary = new Dictionary<string, object>();

            foreach (var key in dict.Keys)
            {
                dictionary[key] = dict.GetValue(key);
            }

            var scan = DeserializeFromDictionary(dictionary);

            if (scan != null)
            {
                scans.Add(scan);
            }
        }

        _logger?.LogDebug(
            "Found {Count} scans for tracking {TrackingNumber}",
            scans.Count,
            trackingNumber);

        return scans;
    }

    public Task<List<ShipmentScan>> GetPendingScansAsync(CancellationToken cancellationToken = default)
    {
        return Task.Run(GetPendingScans, cancellationToken);
    }

    private List<ShipmentScan> GetPendingScans()
    {
        var collection = _database.DefaultCollection;
        var scans = new List<ShipmentScan>();

        using var query = QueryBuilder.Select(
                SelectResult.All())
            .From(DataSource.Collection(collection))
            .Where(Expression.Property("syncStatus")
                .EqualTo(Expression.String("Pending")));

        var resultSet = query.Execute();

        foreach (var result in resultSet.AllResults())
        {
            var dict = result.GetDictionary(0);

            if (dict == null)
                continue;

            var scan = DeserializeFromDictionary(ConvertToDictionary(dict));

            if (scan != null)
            {
                scans.Add(scan);
            }
        }

        _logger?.LogDebug("Found {Count} pending scans", scans.Count);

        return scans;
    }

    private Dictionary<string, object> ConvertToDictionary(DictionaryObject dict)
    {
        var dictionary = new Dictionary<string, object>();

        foreach (var key in dict.Keys)
        {
            dictionary[key] = dict.GetValue(key);
        }

        return dictionary;
    }

    public Task<Dictionary<ScanType, int>> GetScanCountsByTypeAsync(string courierId, CancellationToken cancellationToken = default)
    {
        return Task.Run(() => GetScanCountsByType(courierId), cancellationToken);
    }

    private Dictionary<ScanType, int> GetScanCountsByType(string courierId)
    {
        var collection = _database.DefaultCollection;
        var counts = new Dictionary<ScanType, int>();

        using var query = QueryBuilder.Select(
                SelectResult.Expression(Expression.Property("scanType")),
                SelectResult.Expression(Function.Count(Expression.All())))
            .From(DataSource.Collection(collection))
            .Where(Expression.Property("courierId").EqualTo(Expression.String(courierId)))
            .GroupBy(Expression.Property("scanType"));

        var resultSet = query.Execute();
        foreach (var result in resultSet.AllResults())
        {
            var scanTypeStr = result.GetString(0);
            var count = (int)result.GetInt(1);

            if (Enum.TryParse<ScanType>(scanTypeStr, out var scanType))
            {
                counts[scanType] = count;
            }
        }

        _logger?.LogDebug("Scan counts for courier {CourierId}: {Counts}", courierId, string.Join(", ", counts.Select(kvp => $"{kvp.Key}={kvp.Value}")));
        return counts;
    }

    private ShipmentScan? DeserializeScan(Document document)
    {
        try
        {
            var dict = document.ToDictionary();
            if (!dict.ContainsKey("id") || dict["id"] is null)
            {
                dict["id"] = document.Id.StartsWith("scan::", StringComparison.Ordinal)
                    ? document.Id["scan::".Length..]
                    : document.Id;
            }
            return DeserializeFromDictionary(dict);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to deserialize scan document {DocId}", document.Id);
            return null;
        }
    }

    private ShipmentScan? DeserializeFromDictionary(IDictionary<string, object> dict)
    {
        try
        {
            return new ShipmentScan
            {
                Id = Guid.Parse(dict["id"].ToString()!),
                TrackingNumber = dict["trackingNumber"].ToString()!,
                ScanType = Enum.Parse<ScanType>(dict["scanType"].ToString()!),
                CourierId = dict["courierId"].ToString()!,
                Location = dict["location"].ToString()!,
                ScannedAtUtc = ParseTimestamp(dict["scannedAtUtc"]),
                Notes = dict["notes"]?.ToString() ?? string.Empty,
                SyncStatus = Enum.Parse<SyncStatus>(dict["syncStatus"].ToString()!)
            };
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to deserialize scan from dictionary");
            return null;
        }
    }

    private static DateTimeOffset ParseTimestamp(object? value)
    {
        return value switch
        {
            DateTimeOffset dto => dto,
            DateTime dt => new DateTimeOffset(dt, TimeSpan.Zero),
            string s => DateTimeOffset.Parse(s, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.RoundtripKind),
            _ => DateTimeOffset.MinValue
        };
    }
}
