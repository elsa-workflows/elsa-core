using Elsa.Persistence.VNext.Builders;
using Elsa.Persistence.VNext.Document;
using Elsa.Persistence.VNext.PostgreSql;
using Elsa.Persistence.VNext.Relational.Documents;
using Elsa.Persistence.VNext.SqlServer;
using Microsoft.Data.SqlClient;
using Npgsql;

namespace Elsa.Persistence.VNext.UnitTests;

public class RelationalDocumentProviderTests
{
    [Fact]
    public void SqlServerDocumentStore_UsesSharedRelationalStoreContract()
    {
        using var connection = new SqlConnection();

        var store = new SqlServerDocumentStore(connection, CreateSchema());

        Assert.IsAssignableFrom<IDocumentStore>(store);
        Assert.IsAssignableFrom<RelationalDocumentStore>(store);
    }

    [Fact]
    public void PostgreSqlDocumentStore_UsesSharedRelationalStoreContract()
    {
        using var connection = new NpgsqlConnection();

        var store = new PostgreSqlDocumentStore(connection, CreateSchema());

        Assert.IsAssignableFrom<IDocumentStore>(store);
        Assert.IsAssignableFrom<RelationalDocumentStore>(store);
    }

    [Fact]
    public void SqlServerDialect_RendersProviderSpecificDocumentStorage()
    {
        var dialect = new SqlServerDocumentStoreDialect();
        var locks = string.Join(Environment.NewLine, dialect.CreateMaterializationLockStatements());
        var materialization = string.Join(Environment.NewLine, dialect.CreateMaterializationStatements());
        var upsert = dialect.RenderUpsertDocumentSql();

        Assert.Contains("sp_getapplock", locks, StringComparison.Ordinal);
        Assert.Contains("@LockOwner = N'Transaction'", locks, StringComparison.Ordinal);
        Assert.Contains("IF OBJECT_ID", materialization, StringComparison.Ordinal);
        Assert.Contains("CREATE TABLE [ElsaDocuments]", materialization, StringComparison.Ordinal);
        Assert.Contains("[Content] nvarchar(max) NOT NULL", materialization, StringComparison.Ordinal);
        Assert.Contains("IF @@ROWCOUNT = 0", upsert, StringComparison.Ordinal);
        Assert.Contains("@storageUnit", upsert, StringComparison.Ordinal);
    }

    [Fact]
    public void PostgreSqlDialect_RendersProviderSpecificDocumentStorage()
    {
        var dialect = new PostgreSqlDocumentStoreDialect();
        var locks = string.Join(Environment.NewLine, dialect.CreateMaterializationLockStatements());
        var materialization = string.Join(Environment.NewLine, dialect.CreateMaterializationStatements());
        var upsert = dialect.RenderUpsertDocumentSql();

        Assert.Contains("pg_advisory_xact_lock", locks, StringComparison.Ordinal);
        Assert.Contains("CREATE TABLE IF NOT EXISTS \"ElsaDocuments\"", materialization, StringComparison.Ordinal);
        Assert.Contains("\"Content\" TEXT NOT NULL", materialization, StringComparison.Ordinal);
        Assert.Contains("ON CONFLICT(\"StorageUnit\", \"Id\") DO UPDATE", upsert, StringComparison.Ordinal);
        Assert.Contains("@storageUnit", upsert, StringComparison.Ordinal);
    }

    [Fact]
    public void ProviderDialects_RejectUndeclaredIndexShape()
    {
        var plan = new DocumentDatabasePlanner().Plan(CreateSchema());
        var collection = Assert.Single(plan.Collections);
        var dialect = new PostgreSqlDocumentStoreDialect();
        var query = new DocumentQuery("Orders", new Dictionary<string, string?> { ["Priority"] = "High" });

        Assert.Throws<DocumentQueryNotIndexedException>(() => dialect.FindMatchingIndex(collection, query));
    }

    private static PersistenceSchema CreateSchema()
    {
        return new PersistenceSchemaBuilder("Orders")
            .StorageUnit("Orders", storage => storage
                .RequiredField("Id", PersistenceColumnType.String, 450)
                .RequiredField("Status", PersistenceColumnType.String, 50)
                .RequiredField("CustomerId", PersistenceColumnType.String, 450)
                .Key("PK_Orders", "Id")
                .Index("IX_Orders_Status", "Status")
                .Index("IX_Orders_CustomerId_Status", ["CustomerId", "Status"]))
            .Build();
    }
}
