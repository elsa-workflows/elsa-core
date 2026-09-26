namespace Elsa.Persistence.EFCore.UnitTests;

public sealed class SqliteStoreSaveManyTenantOwnershipTests : StoreSaveManyTenantOwnershipTests
{
    protected override Task<OwnershipStoreScenario> CreateScenarioAsync(string tenantId, bool tenantsEnabled = true) =>
        OwnershipStoreScenario.CreateSqliteAsync(tenantId, tenantsEnabled);
}
