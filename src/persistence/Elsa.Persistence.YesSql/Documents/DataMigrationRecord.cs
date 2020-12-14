namespace Elsa.Persistence.YesSql.Documents
{
    /// <summary>
    /// Represents a database migration.
    /// </summary>
    public class DataMigrationRecord
    {
        /// <summary>
        /// Gets or sets a class for the database migration.
        /// </summary>
        public string DataMigrationClass { get; set; } = default!;

        /// <summary>
        /// Gets or sets the version of the database migration.
        /// </summary>
        public int? Version { get; set; }
    }
}