using Elsa.Common.Multitenancy;
using Elsa.Workflows.Management.Stores;

namespace Elsa.Workflows.Management.UnitTests.Stores;

public class MemoryWorkflowDefinitionRegistryGenerationStoreTests
{
    [Fact]
    public async Task IncrementAsync_KeepsTenantAndAgnosticGenerationsSeparate()
    {
        var store = new MemoryWorkflowDefinitionRegistryGenerationStore();

        Assert.Equal(1, await store.IncrementAsync("tenant-a"));
        Assert.Equal(2, await store.IncrementAsync("tenant-a"));
        Assert.Equal(1, await store.IncrementAsync(Tenant.AgnosticTenantId));
        Assert.Equal(0, await store.GetGenerationAsync("tenant-b"));
        Assert.Equal(2, await store.GetGenerationAsync("tenant-a"));
        Assert.Equal(1, await store.GetGenerationAsync(Tenant.AgnosticTenantId));
    }
}
