using Couchbase.Lite;
using Couchbase.Lite.Query;
using Microsoft.Extensions.Logging;

namespace ApexPost.Courier.Offline;

public sealed class CourierDatabase : IDisposable
{
    private readonly Database _database;
    private readonly ILogger<CourierDatabase>? _logger;
    private ListenerToken? _changeListenerToken;

    public CourierDatabase(string databaseName, ILogger<CourierDatabase>? logger = null)
    {
        _logger = logger;
        
        try
        {
            _database = new Database(databaseName);
            _logger?.LogInformation("Courier database '{DatabaseName}' opened successfully", databaseName);
        }
        catch (CouchbaseLiteException ex)
        {
            _logger?.LogError(ex, "Failed to open courier database '{DatabaseName}'", databaseName);
            throw;
        }
    }

    public Collection DefaultCollection => _database.GetDefaultCollection();

    public void RegisterChangeListener(Action<HashSet<string>> onChanged)
    {
        _changeListenerToken = DefaultCollection.AddChangeListener((sender, changes) =>
        {
            if (changes == null || changes.DocumentIDs.Count == 0)
            {
                return;
            }

            var changedIds = new HashSet<string>(changes.DocumentIDs);
            _logger?.LogInformation("Changed document IDs: {Ids}", string.Join(", ", changedIds));
            onChanged(changedIds);
        });

        _logger?.LogInformation("Change listener registered");
    }

    public void CreateIndexes()
    {
        try
        {
            // Create index on trackingNumber
            var trackingNumberIndex = IndexBuilder.ValueIndex(
                ValueIndexItem.Property("trackingNumber")
            );
            DefaultCollection.CreateIndex("idx_trackingNumber", trackingNumberIndex);
            _logger?.LogInformation("Created index on trackingNumber");

            // Create index on syncStatus
            var syncStatusIndex = IndexBuilder.ValueIndex(
                ValueIndexItem.Property("syncStatus")
            );
            DefaultCollection.CreateIndex("idx_syncStatus", syncStatusIndex);
            _logger?.LogInformation("Created index on syncStatus");
        }
        catch (CouchbaseLiteException ex)
        {
            _logger?.LogError(ex, "Failed to create indexes");
            throw;
        }
    }

    public void Dispose()
    {
        if (_changeListenerToken.HasValue)
        {
            DefaultCollection.RemoveChangeListener(_changeListenerToken.Value);
            _changeListenerToken = null;
            _logger?.LogInformation("Change listener removed");
        }

        _database.Close();
        _logger?.LogInformation("Courier database closed");
    }
}
