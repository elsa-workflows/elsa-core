using Elsa.Persistence.VNext.Document;
using Elsa.Persistence.VNext.Relational.Documents;
using Microsoft.Data.Sqlite;

namespace Elsa.Persistence.VNext.Sqlite;

public class SqliteDocumentStore : RelationalDocumentStore
{
    public SqliteDocumentStore(SqliteConnection connection, PersistenceSchema schema) : base(connection, schema, new SqliteDocumentStoreDialect())
    {
    }

    public SqliteDocumentStore(SqliteConnection connection, DocumentDatabasePlan plan) : base(connection, plan, new SqliteDocumentStoreDialect())
    {
    }
}
