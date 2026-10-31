using Elsa.Common.Multitenancy;
using Elsa.Testing.Shared.Multitenancy;
using Elsa.Workflows.Management.Contracts;
using Elsa.Workflows.Management.Entities;
using Elsa.Workflows.Management.Handlers.Notifications;
using Elsa.Workflows.Management.Notifications;
using Elsa.Workflows.Management.Stores;
using NSubstitute;

namespace Elsa.Workflows.Management.UnitTests.Handlers;

public class RefreshActivityRegistryTests
{
    [Fact]
    public async Task Deletes_IncrementAmbientAndAgnosticGenerations()
    {
        var generations = new MemoryWorkflowDefinitionRegistryGenerationStore();
        var updater = Substitute.For<IWorkflowDefinitionActivityRegistryUpdater>();
        var handler = new RefreshActivityRegistry(updater, generations, new TestTenantAccessor("tenant-a"));

        await handler.HandleAsync(new WorkflowDefinitionDeleted("definition-1"), CancellationToken.None);
        await handler.HandleAsync(new WorkflowDefinitionsDeleted(["definition-2", "definition-3"]), CancellationToken.None);
        await handler.HandleAsync(new WorkflowDefinitionVersionsDeleted(["version-1", "version-2"]), CancellationToken.None);

        Assert.Equal(3, await generations.GetGenerationAsync("tenant-a"));
        Assert.Equal(3, await generations.GetGenerationAsync(Tenant.AgnosticTenantId));
    }

    [Fact]
    public async Task DeleteInAgnosticScope_IncrementsAgnosticGenerationOnlyOnce()
    {
        var generations = new MemoryWorkflowDefinitionRegistryGenerationStore();
        var handler = new RefreshActivityRegistry(
            Substitute.For<IWorkflowDefinitionActivityRegistryUpdater>(),
            generations,
            new TestTenantAccessor(Tenant.AgnosticTenantId));

        await handler.HandleAsync(new WorkflowDefinitionDeleted("definition-1"), CancellationToken.None);

        Assert.Equal(1, await generations.GetGenerationAsync(Tenant.AgnosticTenantId));
        Assert.Equal(0, await generations.GetGenerationAsync(Tenant.DefaultTenantId));
    }

    [Fact]
    public async Task BulkVersionUpdates_IncrementEachDefinitionTenant()
    {
        var generations = new MemoryWorkflowDefinitionRegistryGenerationStore();
        var handler = new RefreshActivityRegistry(
            Substitute.For<IWorkflowDefinitionActivityRegistryUpdater>(),
            generations,
            new TestTenantAccessor("ambient-tenant"));
        var definitions = new[]
        {
            new WorkflowDefinition { Id = "version-1", TenantId = "tenant-a" },
            new WorkflowDefinition { Id = "version-2", TenantId = "tenant-b" },
            new WorkflowDefinition { Id = "version-3", TenantId = Tenant.AgnosticTenantId }
        };

        await handler.HandleAsync(new WorkflowDefinitionVersionsUpdated(definitions), CancellationToken.None);

        Assert.Equal(1, await generations.GetGenerationAsync("tenant-a"));
        Assert.Equal(1, await generations.GetGenerationAsync("tenant-b"));
        Assert.Equal(1, await generations.GetGenerationAsync(Tenant.AgnosticTenantId));
        Assert.Equal(0, await generations.GetGenerationAsync("ambient-tenant"));
    }
}
