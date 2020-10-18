using Microsoft.Data.Sqlite;

namespace Elsa.YesSql.Provider.Sqlite.InMemory
{
    public class InMemorySqliteConnection : SqliteConnection
    {
        protected override void Dispose(bool disposing)
        {
            // Don't dispose anything.
        }
    }
}