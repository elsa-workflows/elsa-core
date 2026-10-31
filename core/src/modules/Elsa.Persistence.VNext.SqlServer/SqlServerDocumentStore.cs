using Elsa.Persistence.VNext.Document;
using Elsa.Persistence.VNext.Relational.Documents;
using Microsoft.Data.SqlClient;

namespace Elsa.Persistence.VNext.SqlServer;

public class SqlServerDocumentStore : RelationalDocumentStore
{
    public SqlServerDocumentStore(SqlConnection connection, PersistenceSchema schema) : base(connection, schema, new SqlServerDocumentStoreDialect())
    {
    }

    public SqlServerDocumentStore(SqlConnection connection, DocumentDatabasePlan plan) : base(connection, plan, new SqlServerDocumentStoreDialect())
    {
    }
}
