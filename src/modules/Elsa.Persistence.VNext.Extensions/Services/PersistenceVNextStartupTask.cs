using Elsa.Common;
using Elsa.Persistence.VNext.Document;
using Elsa.Persistence.VNext.Extensions.Contracts;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Elsa.Persistence.VNext.Extensions.Services;

public class PersistenceVNextStartupTask(
    IEnumerable<IDocumentStore> documentStores,
    IPersistenceSchemaCatalog schemaCatalog,
    IPersistenceVNextStatus status,
    IOptions<PersistenceVNextOptions> options,
    ILogger<PersistenceVNextStartupTask> logger) : IStartupTask
{
    public async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        var schema = schemaCatalog.DescribeSchema();
        var stores = documentStores.ToList();
        var attemptedAt = DateTimeOffset.UtcNow;
        var schemaNames = schemaCatalog.Schemas.Select(x => x.Name).Order(StringComparer.Ordinal).ToList();
        var storageUnits = schema.StorageUnits.Select(x => x.Name).Order(StringComparer.Ordinal).ToList();
        var storeTypes = stores.Select(x => x.GetType().FullName ?? x.GetType().Name).Order(StringComparer.Ordinal).ToList();

        if (!options.Value.MaterializeOnStartup)
        {
            status.RecordSuccess(new(false, true, attemptedAt, null, schemaNames, storageUnits, storeTypes));
            logger.LogInformation("Persistence vNext startup materialization is disabled. {SchemaCount} manifests are registered.", schemaCatalog.Schemas.Count);
            return;
        }

        try
        {
            foreach (var store in stores)
                await store.MaterializeAsync(cancellationToken);

            status.RecordSuccess(new(true, true, attemptedAt, DateTimeOffset.UtcNow, schemaNames, storageUnits, storeTypes));
            logger.LogInformation("Persistence vNext materialized {StorageUnitCount} storage units using {StoreCount} document store providers.", storageUnits.Count, stores.Count);
        }
        catch (Exception e)
        {
            status.RecordFailure(new(true, false, attemptedAt, null, schemaNames, storageUnits, storeTypes, e.Message));
            logger.LogError(e, "Persistence vNext materialization failed.");
            throw;
        }
    }
}
