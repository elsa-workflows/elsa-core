using Elsa.Common.Multitenancy;
using Elsa.Testing.Shared.Multitenancy;
using Elsa.Workflows.Management.Contracts;
using Elsa.Workflows.Management.Stores;
using Elsa.Workflows.Runtime.Models;
using Elsa.Workflows.Runtime.Notifications;
using NSubstitute;

namespace Elsa.Workflows.Runtime.UnitTests.Handlers;

public class RefreshActivityRegistryTests
{
    [Fact]
    public async Task ReloadUnderTenant_AdvancesSharedGenerationForOtherTenants()
    {
        var generations = new MemoryWorkflowDefinitionRegistryGenerationStore();
        var handler = new Elsa.Workflows.Runtime.Handlers.RefreshActivityRegistry(
            Substitute.For<IWorkflowDefinitionActivityRegistryUpdater>(),
            generations,
            new TestTenantAccessor("tenant-a"));

        await handler.HandleAsync(new WorkflowDefinitionsReloaded([
            new ReloadedWorkflowDefinition("definition-1", "version-1", 1, false)
        ]), CancellationToken.None);

        Assert.Equal(1, await generations.GetGenerationAsync("tenant-a"));
        Assert.Equal(1, await generations.GetGenerationAsync(Tenant.AgnosticTenantId));
        Assert.Equal(0, await generations.GetGenerationAsync("tenant-b"));
    }

    [Fact]
    public async Task ReloadUnderAgnosticTenant_AdvancesSharedGenerationOnce()
    {
        var generations = new MemoryWorkflowDefinitionRegistryGenerationStore();
        var handler = new Elsa.Workflows.Runtime.Handlers.RefreshActivityRegistry(
            Substitute.For<IWorkflowDefinitionActivityRegistryUpdater>(),
            generations,
            new TestTenantAccessor(Tenant.AgnosticTenantId));

        await handler.HandleAsync(new WorkflowDefinitionsReloaded([]), CancellationToken.None);

        Assert.Equal(1, await generations.GetGenerationAsync(Tenant.AgnosticTenantId));
    }
}
