using Elsa.Mediator.Contracts;
using Elsa.Workflows.Management.Entities;
using Elsa.Workflows.Management.Filters;
using Elsa.Workflows.Management.Models;
using Elsa.Workflows.Management.Services;
using NSubstitute;

namespace Elsa.Workflows.Management.UnitTests.Services;

public class WorkflowInstanceManagerTenantAgnosticDeleteTests
{
    [Fact(DisplayName = "DeleteAsync passes a tenant-scoped filter to the store delete")]
    public async Task DeleteAsync_TenantAgnostic_PassesTenantScopedFilterToStore()
    {
        var store = Substitute.For<IWorkflowInstanceStore>();
        store.FindAsync(Arg.Any<WorkflowInstanceFilter>(), Arg.Any<CancellationToken>()).Returns(new ValueTask<WorkflowInstance?>(new WorkflowInstance { Id = "i-t1" }));
        var manager = new WorkflowInstanceManager(store, null!, Substitute.For<INotificationSender>(), null!, null!, null!);

        await manager.DeleteAsync(new() { Id = "i-t1", TenantAgnostic = true });

        await store.Received(1).DeleteAsync(Arg.Is<WorkflowInstanceFilter>(f => !f.TenantAgnostic && f.Id == "i-t1"), Arg.Any<CancellationToken>());
    }

    [Fact(DisplayName = "BulkDeleteAsync passes a tenant-scoped filter to the store delete")]
    public async Task BulkDeleteAsync_TenantAgnostic_PassesTenantScopedFilterToStore()
    {
        var store = Substitute.For<IWorkflowInstanceStore>();
        store.SummarizeManyAsync(Arg.Any<WorkflowInstanceFilter>(), Arg.Any<CancellationToken>()).Returns(new ValueTask<IEnumerable<WorkflowInstanceSummary>>([]));
        var manager = new WorkflowInstanceManager(store, null!, Substitute.For<INotificationSender>(), null!, null!, null!);

        await manager.BulkDeleteAsync(new() { TenantAgnostic = true });

        await store.Received(1).DeleteAsync(Arg.Is<WorkflowInstanceFilter>(f => !f.TenantAgnostic), Arg.Any<CancellationToken>());
    }
}
