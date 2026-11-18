using Elsa.Common.Multitenancy;
using Microsoft.EntityFrameworkCore;

namespace Elsa.Persistence.EFCore.UnitTests;

public sealed class NativeStoreSharedOwnershipTests
{
    private const string PostgreSqlConnectionVariable = "ELSA_EF_OWNERSHIP_POSTGRES_TEST_CONNECTION";
    private const string SqlServerConnectionVariable = "ELSA_EF_OWNERSHIP_SQLSERVER_TEST_CONNECTION";
    private const string OracleConnectionVariable = "ELSA_EF_OWNERSHIP_ORACLE_TEST_CONNECTION";

    [NativePostgreSqlFact]
    public async Task PostgreSqlRequiresExplicitHostAuthorityForSharedRows()
    {
        await using var scenario = await OwnershipStoreScenario.CreatePostgreSqlAsync(
            Environment.GetEnvironmentVariable(PostgreSqlConnectionVariable)!, "named", tenantsEnabled: true);
        await VerifyOwnershipAsync(scenario);
    }

    [NativeSqlServerFact]
    public async Task SqlServerRequiresExplicitHostAuthorityForSharedRows()
    {
        var connectionString = Environment.GetEnvironmentVariable(SqlServerConnectionVariable)!;
        await using var scenario = await OwnershipStoreScenario.CreateAsync("named", true, builder => builder.UseSqlServer(connectionString),
            () => ValueTask.CompletedTask);
        await VerifyOwnershipAsync(scenario);
    }

    [NativeOracleFact]
    public async Task OracleRequiresExplicitHostAuthorityForSharedRows()
    {
        var connectionString = Environment.GetEnvironmentVariable(OracleConnectionVariable)!;
        await using var scenario = await OwnershipStoreScenario.CreateAsync("named", true, builder => builder.UseOracle(connectionString),
            () => ValueTask.CompletedTask);
        await VerifyOwnershipAsync(scenario);
    }

    private static async Task VerifyOwnershipAsync(OwnershipStoreScenario scenario)
    {
        var record = new OwnedRow { Id = Guid.NewGuid().ToString("N"), TenantId = Tenant.AgnosticTenantId, Payload = "first" };

        await Assert.ThrowsAsync<InvalidOperationException>(() => scenario.Store.SaveManyAsync([record], row => row.Id, onSaving: null));

        using (SharedTenantWriteScope.Enter<OwnedRow>())
        {
            await scenario.Store.SaveManyAsync([record], row => row.Id, onSaving: null);
            record.Payload = "updated";
            await scenario.Store.SaveManyAsync([record], row => row.Id, onSaving: null);
        }

        record.Payload = "outside scope";
        await Assert.ThrowsAsync<InvalidOperationException>(() => scenario.Store.SaveManyAsync([record], row => row.Id, onSaving: null));
        Assert.Equal("updated", (await scenario.FindAsync(record.Id))?.Payload);
    }

    public sealed class NativeSqlServerFactAttribute : FactAttribute
    {
        public NativeSqlServerFactAttribute()
        {
            if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(SqlServerConnectionVariable)))
                Skip = $"Set {SqlServerConnectionVariable} to a disposable SQL Server database.";
        }
    }

    public sealed class NativePostgreSqlFactAttribute : FactAttribute
    {
        public NativePostgreSqlFactAttribute()
        {
            if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(PostgreSqlConnectionVariable)))
                Skip = $"Set {PostgreSqlConnectionVariable} to a disposable PostgreSQL server.";
        }
    }

    public sealed class NativeOracleFactAttribute : FactAttribute
    {
        public NativeOracleFactAttribute()
        {
            if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(OracleConnectionVariable)))
                Skip = $"Set {OracleConnectionVariable} to a disposable Oracle schema.";
        }
    }
}
