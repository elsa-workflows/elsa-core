using Elsa.Common.Multitenancy;
using Elsa.Common.Services;
using Elsa.Testing.Shared.Multitenancy;
using Elsa.Workflows.Activities;
using Elsa.Workflows.Management.Entities;
using Elsa.Workflows.Management.Filters;
using Elsa.Workflows.Management.Stores;
using Elsa.Workflows.Runtime.ActivationValidators;

namespace Elsa.Workflows.Runtime.UnitTests.Stores;

public class MemoryWorkflowInstanceStoreTenantIsolationTests
{
    [Fact]
    public async Task ActivationStrategyCountsOnlyCurrentTenantAndSaveStampsAmbientTenant()
    {
        var backingStore = new MemoryStore<WorkflowInstance>();
        var tenantA = new MemoryWorkflowInstanceStore(backingStore, new TestTenantAccessor("tenant-a"));
        var tenantB = new MemoryWorkflowInstanceStore(backingStore, new TestTenantAccessor("tenant-b"));
        var instance = RunningInstance("instance-a", correlationId: "conversation-1", tenantId: null);
        await tenantA.SaveAsync(instance);

        Assert.Equal("tenant-a", instance.TenantId);
        Assert.Equal(1, await tenantA.CountAsync(RunningConversation("conversation-1")));
        Assert.Equal(0, await tenantB.CountAsync(RunningConversation("conversation-1")));

        var workflow = new Workflow();
        var allowedInTenantA = await new CorrelationStrategy(tenantA).GetAllowActivationAsync(new(workflow, "conversation-1", CancellationToken.None));
        var allowedInTenantB = await new CorrelationStrategy(tenantB).GetAllowActivationAsync(new(workflow, "conversation-1", CancellationToken.None));

        Assert.False(allowedInTenantA);
        Assert.True(allowedInTenantB);
        Assert.Equal(1, await tenantA.CountAsync(RunningConversation("conversation-1")));
        Assert.Equal(0, await tenantB.CountAsync(RunningConversation("conversation-1")));
    }

    [Fact]
    public async Task AgnosticRowsRemainVisibleWithoutChangingTheirTenant()
    {
        var backingStore = new MemoryStore<WorkflowInstance>();
        var tenantA = new MemoryWorkflowInstanceStore(backingStore, new TestTenantAccessor("tenant-a"));
        var tenantB = new MemoryWorkflowInstanceStore(backingStore, new TestTenantAccessor("tenant-b"));
        var shared = RunningInstance("instance-shared", "shared-correlation", Tenant.AgnosticTenantId);
        await tenantA.SaveAsync(shared);

        Assert.Equal(Tenant.AgnosticTenantId, shared.TenantId);
        Assert.Equal(1, await tenantA.CountAsync(RunningConversation("shared-correlation")));
        Assert.Equal(1, await tenantB.CountAsync(RunningConversation("shared-correlation")));
    }

    private static WorkflowInstance RunningInstance(string id, string correlationId, string? tenantId) => new()
    {
        Id = id,
        TenantId = tenantId,
        CorrelationId = correlationId,
        DefinitionId = "conversation-workflow",
        DefinitionVersionId = "conversation-workflow-v1",
        Status = WorkflowStatus.Running
    };

    private static WorkflowInstanceFilter RunningConversation(string correlationId) => new()
    {
        CorrelationId = correlationId,
        WorkflowStatus = WorkflowStatus.Running
    };
}
