namespace Elsa.Persistence.VNext.Builders;

public class PersistenceTableBuilder(string name, string? schema)
{
    private readonly List<PersistenceColumn> _columns = [];
    private readonly List<PersistenceIndex> _indexes = [];
    private PersistencePrimaryKey? _primaryKey;

    public PersistenceTableBuilder Column(string name, PersistenceColumnType type, bool nullable = true, int? length = null)
    {
        _columns.Add(new PersistenceColumn(name, type, nullable, length));
        return this;
    }

    public PersistenceTableBuilder RequiredColumn(string name, PersistenceColumnType type, int? length = null)
    {
        return Column(name, type, false, length);
    }

    public PersistenceTableBuilder PrimaryKey(string name, params string[] columns)
    {
        _primaryKey = new PersistencePrimaryKey(name, columns.ToList());
        return this;
    }

    public PersistenceTableBuilder Index(string name, string column, bool unique = false)
    {
        return Index(name, [column], unique);
    }

    public PersistenceTableBuilder Index(string name, IReadOnlyList<string> columns, bool unique = false)
    {
        _indexes.Add(new PersistenceIndex(name, columns.ToList(), unique));
        return this;
    }

    public PersistenceTable Build()
    {
        return new PersistenceTable(Name: name, Schema: schema, Columns: _columns.ToList(), PrimaryKey: _primaryKey, Indexes: _indexes.ToList());
    }
}
