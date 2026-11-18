using Elsa.Common.Entities;
using Elsa.Persistence.EFCore.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;

namespace Elsa.Persistence.EFCore.UnitTests;

public class ProviderStoreWriteConcurrencyTests
{
    private const string SqlServer = "ELSA_SQLSERVER_CONNECTION_STRING";
    private const string Oracle = "ELSA_ORACLE_CONNECTION_STRING";
    private const string MySql = "ELSA_MYSQL_CONNECTION_STRING";

    [ProviderDatabase(SqlServer)]
    public Task SqlServer_VerifiesTextKeyAndFallsBackForOtherCollations() =>
        CheckAsync(SqlServer, (b, cs) => b.UseElsaSqlServer(GetType().Assembly, cs),
            "nvarchar(128) COLLATE Latin1_General_100_CI_AS", "nvarchar(128) COLLATE Turkish_CI_AS");

    [ProviderDatabase(Oracle)]
    public Task Oracle_VerifiesNationalTextKeyAndFallsBackForOtherTypes() =>
        CheckAsync(Oracle, (b, cs) => b.UseElsaOracle(GetType().Assembly, cs),
            "NVARCHAR2(128)", "VARCHAR2(128)");

#if NET9_0
    [ProviderDatabase(MySql)]
    public Task MySql_VerifiesTextKeyAndFallsBackForOtherCollations() =>
        CheckAsync(MySql, (b, cs) => b.UseElsaMySql(GetType().Assembly, cs),
            "varchar(128) CHARACTER SET utf8mb4 COLLATE utf8mb4_bin",
            "varchar(128) CHARACTER SET utf8mb4 COLLATE utf8mb4_turkish_ci");
#endif

    private static async Task CheckAsync(string variable, Action<DbContextOptionsBuilder, string> configure,
        string verifiedColumnType, string unknownColumnType)
    {
        var builder = new DbContextOptionsBuilder().EnableServiceProviderCaching(false);
        configure(builder, Environment.GetEnvironmentVariable(variable)!);
        builder.UseElsaDbContextOptions(new ElsaDbContextOptions());
        var tableName = "ew_" + Guid.NewGuid().ToString("N")[..24];
        await using var db = new TestContext(builder.Options, tableName);
        var sql = db.GetService<ISqlGenerationHelper>();
        var table = sql.DelimitIdentifier(tableName);
        var id = sql.DelimitIdentifier(nameof(Entity.Id));
        var coordinator = new StoreWriteLock();

        foreach (var type in new[] { verifiedColumnType, unknownColumnType })
        {
            var ddl = $"CREATE TABLE {table} ({id} {type} NOT NULL PRIMARY KEY)";
            await db.Database.ExecuteSqlRawAsync(ddl);
            try
            {
                using var first = await AcquireAsync("Order-A", CancellationToken.None);
                using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(15));
                if (type == verifiedColumnType)
                {
                    using var independent = await AcquireAsync("Order-B", timeout.Token);
                }

                using var cancel = new CancellationTokenSource(TimeSpan.FromMilliseconds(500));
                var conflicting = type == verifiedColumnType ? "order-a" : "Order-B";
                await Assert.ThrowsAnyAsync<OperationCanceledException>(
                    () => AcquireAsync(conflicting, cancel.Token).AsTask());
            }
            finally
            {
                var drop = $"DROP TABLE {table}";
                await db.Database.ExecuteSqlRawAsync(drop);
            }
        }

        ValueTask<IDisposable> AcquireAsync(string value, CancellationToken ct) =>
            coordinator.AcquireByIdAsync(db, new[] { new Row { Id = value } }, ct);
    }

    private sealed class Row : Entity;

    private sealed class TestContext(DbContextOptions options, string tableName) : DbContext(options)
    {
        protected override void OnModelCreating(ModelBuilder builder) => builder.Entity<Row>().ToTable(tableName);
    }

    private sealed class ProviderDatabaseAttribute : FactAttribute
    {
        public ProviderDatabaseAttribute(string variable)
        {
            if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(variable)))
            {
                Skip = $"Set {variable} to a test database where the test can create and drop its own table.";
            }
        }
    }
}
