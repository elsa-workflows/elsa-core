using YesSql.Provider.Sqlite;

namespace Elsa.YesSql.Provider.Sqlite.InMemory
{
    public class InMemorySqliteDialect : SqliteDialect
    {
        public override string Name => "InMemorySqlite";
    }
}
