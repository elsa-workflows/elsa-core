using Elsa.Persistence.VNext.Relational;

namespace Elsa.Persistence.VNext.Sqlite;

public class SqliteTypeMapper : IRelationalTypeMapper
{
    public string Map(PersistenceColumn column)
    {
        return column.Type switch
        {
            PersistenceColumnType.Int32 or PersistenceColumnType.Int64 or PersistenceColumnType.Boolean => "INTEGER",
            PersistenceColumnType.String or PersistenceColumnType.Text or PersistenceColumnType.Json or PersistenceColumnType.DateTimeOffset => "TEXT",
            _ => throw new ArgumentOutOfRangeException(nameof(column), column.Type, "Unsupported persistence column type.")
        };
    }
}
