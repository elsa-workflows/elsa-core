namespace Elsa.Persistence.VNext.Builders;

public class PersistenceSchemaBuilder(string name)
{
    private readonly List<PersistenceTable> _tables = [];
    private readonly List<PersistenceStorageUnit> _storageUnits = [];
    private int _version = 1;

    public PersistenceSchemaBuilder Version(int version)
    {
        if (version < 1)
            throw new ArgumentOutOfRangeException(nameof(version), version, "Schema version must be greater than zero.");

        _version = version;
        return this;
    }

    public PersistenceSchemaBuilder Table(string name, Action<PersistenceTableBuilder> configure, string? schema = null)
    {
        var builder = new PersistenceTableBuilder(name, schema);
        configure(builder);
        _tables.Add(builder.Build());
        return this;
    }

    public PersistenceSchemaBuilder StorageUnit(string name, Action<PersistenceStorageUnitBuilder> configure, string? @namespace = null)
    {
        var builder = new PersistenceStorageUnitBuilder(name, @namespace);
        configure(builder);
        _storageUnits.Add(builder.Build());
        return this;
    }

    public PersistenceSchema Build()
    {
        return new PersistenceSchema(Name: name, Version: _version, Tables: _tables.ToList(), StorageUnits: _storageUnits.ToList());
    }
}
