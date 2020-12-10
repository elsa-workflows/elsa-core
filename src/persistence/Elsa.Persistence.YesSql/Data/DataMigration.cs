using YesSql.Sql;

namespace Elsa.Persistence.YesSql.Data
{
    /// <summary>
    /// Represents a database migration.
    /// </summary>
    public abstract class DataMigration : IDataMigration
    {
        public ISchemaBuilder SchemaBuilder { get; set; } = default!;
    }
}
