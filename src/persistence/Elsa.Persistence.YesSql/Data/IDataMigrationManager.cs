using System.Threading.Tasks;

namespace Elsa.Persistence.YesSql.Data
{
    /// <summary>
    /// Represents a contract to manage database migrations.
    /// </summary>
    public interface IDataMigrationManager
    {
        /// <summary>
        /// Run all migrations that need to be updated.
        /// </summary>
        Task RunAllAsync();
    }
}