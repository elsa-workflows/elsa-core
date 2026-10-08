using Elsa.Persistence.VNext.Contracts;
using Elsa.Persistence.VNext.Extensions.Contracts;
using Microsoft.Extensions.Options;

namespace Elsa.Persistence.VNext.Extensions.Services;

public class DefaultPersistenceSchemaCatalog(IEnumerable<IPersistenceSchemaProvider> providers, IOptions<PersistenceVNextOptions> options) : IPersistenceSchemaCatalog
{
    private readonly IReadOnlyList<IPersistenceSchemaProvider> _providers = providers.ToList();
    private IReadOnlyList<PersistenceSchema>? _schemas;

    public IReadOnlyList<IPersistenceSchemaProvider> Providers => _providers;
    public IReadOnlyList<PersistenceSchema> Schemas => _schemas ??= _providers.Select(x => x.DescribeSchema()).ToList();

    public PersistenceSchema DescribeSchema()
    {
        var schemas = Schemas;
        var tables = schemas.SelectMany(x => x.Tables).ToList();
        var storageUnits = schemas.SelectMany(x => x.StorageUnits).ToList();

        EnsureUniqueTables(tables);
        EnsureUniqueStorageUnits(storageUnits);

        return new PersistenceSchema(options.Value.SchemaName, options.Value.SchemaVersion, tables, storageUnits);
    }

    private static void EnsureUniqueTables(IEnumerable<PersistenceTable> tables)
    {
        var duplicate = tables
            .GroupBy(x => $"{x.Schema ?? string.Empty}.{x.Name}", StringComparer.Ordinal)
            .FirstOrDefault(x => x.Count() > 1);

        if (duplicate is not null)
            throw new InvalidOperationException($"Persistence vNext table '{duplicate.Key}' is declared more than once.");
    }

    private static void EnsureUniqueStorageUnits(IEnumerable<PersistenceStorageUnit> storageUnits)
    {
        var duplicate = storageUnits
            .GroupBy(x => $"{x.Namespace ?? string.Empty}.{x.Name}", StringComparer.Ordinal)
            .FirstOrDefault(x => x.Count() > 1);

        if (duplicate is not null)
            throw new InvalidOperationException($"Persistence vNext storage unit '{duplicate.Key}' is declared more than once.");
    }
}
