using System.Collections.Generic;

namespace Elsa.Persistence.YesSql.Documents
{
    /// <summary>
    /// Represents a record in the database migration.
    /// </summary>
    public class DataMigrationsDocument : YesSqlDocument
    {
        public DataMigrationsDocument()
        {
            DataMigrations = new List<DataMigrationRecord>();
        }

        public List<DataMigrationRecord> DataMigrations { get; set; }
    }
}