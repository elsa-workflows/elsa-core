using Elsa.Persistence.VNext.Relational;

namespace Elsa.Persistence.VNext.SqlServer;

public class SqlServerTypeMapper : IRelationalTypeMapper
{
    public string Map(PersistenceColumn column)
    {
        return column.Type switch
        {
            PersistenceColumnType.Int32 => "int",
            PersistenceColumnType.Int64 => "bigint",
            PersistenceColumnType.Boolean => "bit",
            PersistenceColumnType.DateTimeOffset => "datetimeoffset",
            PersistenceColumnType.Text or PersistenceColumnType.Json => "nvarchar(max)",
            PersistenceColumnType.String => column.Length is { } length ? $"nvarchar({length})" : "nvarchar(max)",
            _ => throw new ArgumentOutOfRangeException(nameof(column), column.Type, "Unsupported persistence column type.")
        };
    }
}
