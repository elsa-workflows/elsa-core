using Elsa.Persistence.VNext.Document;
using Elsa.Persistence.VNext.Relational.Documents;
using Npgsql;

namespace Elsa.Persistence.VNext.PostgreSql;

public class PostgreSqlDocumentStore : RelationalDocumentStore
{
    public PostgreSqlDocumentStore(NpgsqlConnection connection, PersistenceSchema schema) : base(connection, schema, new PostgreSqlDocumentStoreDialect())
    {
    }

    public PostgreSqlDocumentStore(NpgsqlConnection connection, DocumentDatabasePlan plan) : base(connection, plan, new PostgreSqlDocumentStoreDialect())
    {
    }
}
