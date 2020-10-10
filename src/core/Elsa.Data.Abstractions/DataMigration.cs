using YesSql.Sql;

namespace Elsa.Persistence
{
    /// <summary>
    /// Represents a database migration.
    /// </summary>
    public abstract class DataMigration : IDataMigration
    {
        public ISchemaBuilder SchemaBuilder { get; set; }
    }
}