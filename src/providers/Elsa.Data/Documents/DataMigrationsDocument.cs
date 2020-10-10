using System.Collections.Generic;

namespace Elsa.Data.Documents
{
    /// <summary>
    /// Represents a record in the database migration.
    /// </summary>
    public class DataMigrationsDocument
    {
        public DataMigrationsDocument()
        {
            DataMigrations = new List<DataMigrationRecord>();
        }

        public int Id { get; set; }

        public List<DataMigrationRecord> DataMigrations { get; set; }
    }
}