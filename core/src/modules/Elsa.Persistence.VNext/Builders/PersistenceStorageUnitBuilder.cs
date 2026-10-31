namespace Elsa.Persistence.VNext.Builders;

public class PersistenceStorageUnitBuilder(string name, string? @namespace)
{
    private readonly List<PersistenceField> _fields = [];
    private readonly List<PersistenceIndex> _indexes = [];
    private PersistencePrimaryKey? _key;

    public PersistenceStorageUnitBuilder Field(string name, PersistenceColumnType type, bool nullable = true, int? length = null)
    {
        _fields.Add(new PersistenceField(name, type, nullable, length));
        return this;
    }

    public PersistenceStorageUnitBuilder RequiredField(string name, PersistenceColumnType type, int? length = null)
    {
        return Field(name, type, false, length);
    }

    public PersistenceStorageUnitBuilder Key(string name, params string[] fields)
    {
        _key = new PersistencePrimaryKey(name, fields.ToList());
        return this;
    }

    public PersistenceStorageUnitBuilder Index(string name, string field, bool unique = false)
    {
        return Index(name, [field], unique);
    }

    public PersistenceStorageUnitBuilder Index(string name, IReadOnlyList<string> fields, bool unique = false)
    {
        _indexes.Add(new PersistenceIndex(name, fields.ToList(), unique));
        return this;
    }

    public PersistenceStorageUnit Build()
    {
        return new PersistenceStorageUnit(Name: name, Namespace: @namespace, Fields: _fields.ToList(), Key: _key, Indexes: _indexes.ToList());
    }
}
