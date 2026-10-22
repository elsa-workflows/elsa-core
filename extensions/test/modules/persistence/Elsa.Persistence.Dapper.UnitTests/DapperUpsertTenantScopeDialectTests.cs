using Elsa.Persistence.Dapper.Contracts;
using Elsa.Persistence.Dapper.Dialects;

namespace Elsa.Persistence.Dapper.UnitTests;

/// <summary>
/// Tenant-scoped upsert SQL for each Dapper dialect (#9010). No database required.
/// </summary>
public sealed class DapperUpsertTenantScopeDialectTests
{
    [Fact]
    public void Sqlite_ReplacesInsertOrReplace_AndScopesUpdateToTenantId()
    {
        var sql = new SqliteDialect().Upsert("Items", "Id", ["Name", "TenantId"]);

        Assert.DoesNotContain("INSERT OR REPLACE", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("ON CONFLICT(Id) DO UPDATE SET", sql, StringComparison.Ordinal);
        Assert.Contains("WHERE COALESCE(TenantId, '') = COALESCE(@TenantId, '')", sql, StringComparison.Ordinal);
    }

    [Fact]
    public void PostgreSql_ScopesOnConflictUpdateToTenantId()
    {
        var sql = new PostgreSqlDialect().Upsert("Items", "Id", ["Name", "TenantId"]);

        Assert.Contains("on conflict(\"Id\") do update set", sql, StringComparison.Ordinal);
        Assert.Contains("WHERE COALESCE(\"TenantId\", '') = COALESCE(@TenantId, '')", sql, StringComparison.Ordinal);
    }

    [Fact]
    public void SqlServer_ScopesMergeMatchToTenantId()
    {
        var sql = new SqlServerDialect().Upsert("Items", "Id", ["Name", "TenantId"]);

        Assert.Contains("ON Target.Id = Source.Id", sql, StringComparison.Ordinal);
        Assert.Contains("WHEN MATCHED AND COALESCE(Target.TenantId, '') = COALESCE(Source.TenantId, '') THEN", sql, StringComparison.Ordinal);
        Assert.Contains("WHEN NOT MATCHED THEN", sql, StringComparison.Ordinal);
    }

    [Fact]
    public void Upsert_WithoutTenantId_DoesNotAddTenantPredicate()
    {
        ISqlDialect[] dialects = [new SqliteDialect(), new PostgreSqlDialect(), new SqlServerDialect()];

        foreach (var dialect in dialects)
        {
            var sql = dialect.Upsert("Items", "Id", ["Name"]);
            Assert.DoesNotContain("TenantId", sql, StringComparison.Ordinal);
        }
    }
}
