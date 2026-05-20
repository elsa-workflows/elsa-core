using Elsa.Workflows.Management;
using Elsa.Workflows.Management.Filters;
using Elsa.Workflows.Runtime.Filters;
using Elsa.Workflows.Runtime.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using NSubstitute;

namespace Elsa.Workflows.Runtime.UnitTests.HealthChecks;

public class ElsaWorkflowPersistenceHealthCheckTests
{
    private readonly IWorkflowDefinitionStore _workflowDefinitionStore = Substitute.For<IWorkflowDefinitionStore>();
    private readonly IWorkflowInstanceStore _workflowInstanceStore = Substitute.For<IWorkflowInstanceStore>();
    private readonly ITriggerStore _triggerStore = Substitute.For<ITriggerStore>();
    private readonly IBookmarkQueueStore _bookmarkQueueStore = Substitute.For<IBookmarkQueueStore>();
    private readonly ElsaWorkflowPersistenceHealthCheck _sut;

    public ElsaWorkflowPersistenceHealthCheckTests()
    {
        _workflowDefinitionStore.FindAsync(Arg.Any<WorkflowDefinitionFilter>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult<Elsa.Workflows.Management.Entities.WorkflowDefinition?>(null));
        _workflowInstanceStore.CountAsync(Arg.Any<WorkflowInstanceFilter>(), Arg.Any<CancellationToken>()).Returns(new ValueTask<long>(0));
        _triggerStore.FindAsync(Arg.Any<TriggerFilter>(), Arg.Any<CancellationToken>()).Returns(new ValueTask<Elsa.Workflows.Runtime.Entities.StoredTrigger?>((Elsa.Workflows.Runtime.Entities.StoredTrigger?)null));
        _bookmarkQueueStore.FindAsync(Arg.Any<BookmarkQueueFilter>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult<Elsa.Workflows.Runtime.Entities.BookmarkQueueItem?>(null));
        _sut = new ElsaWorkflowPersistenceHealthCheck(_workflowDefinitionStore, _workflowInstanceStore, _triggerStore, _bookmarkQueueStore);
    }

    [Fact]
    public async Task ReturnsHealthyWhenAllStoresCanBeRead()
    {
        var result = await _sut.CheckHealthAsync(new HealthCheckContext());

        Assert.Equal(HealthStatus.Healthy, result.Status);
        Assert.Equal("persistence", result.Data["category"]);
    }

    [Fact]
    public async Task ReturnsUnhealthyWhenAStoreProbeFails()
    {
        _triggerStore.FindAsync(Arg.Any<TriggerFilter>(), Arg.Any<CancellationToken>()).Returns<ValueTask<Elsa.Workflows.Runtime.Entities.StoredTrigger?>>(_ => throw new InvalidOperationException("store unavailable"));

        var result = await _sut.CheckHealthAsync(new HealthCheckContext());

        Assert.Equal(HealthStatus.Unhealthy, result.Status);
        Assert.Equal("persistence", result.Data["category"]);
    }
}
