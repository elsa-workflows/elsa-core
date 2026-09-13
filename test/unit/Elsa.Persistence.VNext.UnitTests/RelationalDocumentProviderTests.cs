using Elsa.Persistence.VNext.Builders;
using Elsa.Persistence.VNext.Document;
using Elsa.Persistence.VNext.PostgreSql;
using Elsa.Persistence.VNext.Relational.Documents;
using Elsa.Persistence.VNext.SqlServer;
using Microsoft.Data.SqlClient;
using Npgsql;
using System.Threading.Tasks;

namespace Elsa.Persistence.VNext.UnitTests;

public class RelationalDocumentProviderTests
{
    [Test]
    public async Task SqlServerDocumentStore_UsesSharedRelationalStoreContract()
    {
        using var connection = new SqlConnection();

        var store = new SqlServerDocumentStore(connection, CreateSchema());

        await Assert.That(store).IsAssignableTo<IDocumentStore>();
        await Assert.That(store).IsAssignableTo<RelationalDocumentStore>();
    }

    [Test]
    public async Task PostgreSqlDocumentStore_UsesSharedRelationalStoreContract()
    {
        using var connection = new NpgsqlConnection();

        var store = new PostgreSqlDocumentStore(connection, CreateSchema());

        await Assert.That(store).IsAssignableTo<IDocumentStore>();
        await Assert.That(store).IsAssignableTo<RelationalDocumentStore>();
    }

    [Test]
    public async Task SqlServerDialect_RendersProviderSpecificDocumentStorage()
    {
        var dialect = new SqlServerDocumentStoreDialect();
        var locks = string.Join(Environment.NewLine, dialect.CreateMaterializationLockStatements());
        var materialization = string.Join(Environment.NewLine, dialect.CreateMaterializationStatements());
        var upsert = dialect.RenderUpsertDocumentSql();

        await Assert.That(locks).Contains("sp_getapplock").WithComparison(StringComparison.Ordinal);
        await Assert.That(locks).Contains("@LockOwner = N'Transaction'").WithComparison(StringComparison.Ordinal);
        await Assert.That(materialization).Contains("IF OBJECT_ID").WithComparison(StringComparison.Ordinal);
        await Assert.That(materialization).Contains("CREATE TABLE [ElsaDocuments]").WithComparison(StringComparison.Ordinal);
        await Assert.That(materialization).Contains("[Content] nvarchar(max) NOT NULL").WithComparison(StringComparison.Ordinal);
        await Assert.That(upsert).Contains("IF @@ROWCOUNT = 0").WithComparison(StringComparison.Ordinal);
        await Assert.That(upsert).Contains("@storageUnit").WithComparison(StringComparison.Ordinal);
    }

    [Test]
    public async Task PostgreSqlDialect_RendersProviderSpecificDocumentStorage()
    {
        var dialect = new PostgreSqlDocumentStoreDialect();
        var locks = string.Join(Environment.NewLine, dialect.CreateMaterializationLockStatements());
        var materialization = string.Join(Environment.NewLine, dialect.CreateMaterializationStatements());
        var upsert = dialect.RenderUpsertDocumentSql();

        await Assert.That(locks).Contains("pg_advisory_xact_lock").WithComparison(StringComparison.Ordinal);
        await Assert.That(materialization).Contains("CREATE TABLE IF NOT EXISTS \"ElsaDocuments\"").WithComparison(StringComparison.Ordinal);
        await Assert.That(materialization).Contains("\"Content\" TEXT NOT NULL").WithComparison(StringComparison.Ordinal);
        await Assert.That(upsert).Contains("ON CONFLICT(\"StorageUnit\", \"Id\") DO UPDATE").WithComparison(StringComparison.Ordinal);
        await Assert.That(upsert).Contains("@storageUnit").WithComparison(StringComparison.Ordinal);
    }

    [Test]
    public async Task ProviderDialects_RejectUndeclaredIndexShape()
    {
        var plan = new DocumentDatabasePlanner().Plan(CreateSchema());
        var collection = await Assert.That(plan.Collections).HasSingleItem();
        var dialect = new PostgreSqlDocumentStoreDialect();
        var query = new DocumentQuery("Orders", new Dictionary<string, string?> { ["Priority"] = "High" });

        Assert.ThrowsExactly<DocumentQueryNotIndexedException>(() => dialect.FindMatchingIndex(collection, query));
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
