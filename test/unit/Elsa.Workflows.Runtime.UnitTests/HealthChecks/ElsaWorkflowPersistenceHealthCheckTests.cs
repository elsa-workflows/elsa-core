using Elsa.Workflows.Management;
using Elsa.Workflows.Management.Filters;
using Elsa.Workflows.Runtime.Filters;
using Elsa.Workflows.Runtime.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace Elsa.Workflows.Runtime.UnitTests.HealthChecks;

public class ElsaWorkflowPersistenceHealthCheckTests
{
    private readonly IServiceProvider _serviceProvider = Substitute.For<IServiceProvider>();
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
        _serviceProvider.GetService(typeof(IWorkflowDefinitionStore)).Returns(_workflowDefinitionStore);
        _serviceProvider.GetService(typeof(IWorkflowInstanceStore)).Returns(_workflowInstanceStore);
        _serviceProvider.GetService(typeof(ITriggerStore)).Returns(_triggerStore);
        _serviceProvider.GetService(typeof(IBookmarkQueueStore)).Returns(_bookmarkQueueStore);
        _sut = new ElsaWorkflowPersistenceHealthCheck(_serviceProvider, NullLogger<ElsaWorkflowPersistenceHealthCheck>.Instance);
    }

    [Fact]
    public async Task ReturnsHealthyWhenAllStoresCanBeRead()
    {
        var result = await _sut.CheckHealthAsync(new HealthCheckContext());

        Assert.Equal(HealthStatus.Healthy, result.Status);
        Assert.Equal("persistence", result.Data["category"]);
    }

    [Fact]
    public async Task ReturnsUnhealthyWithFailedStoreWhenAStoreProbeFails()
    {
        _triggerStore.FindAsync(Arg.Any<TriggerFilter>(), Arg.Any<CancellationToken>()).Returns<ValueTask<Elsa.Workflows.Runtime.Entities.StoredTrigger?>>(_ => throw new InvalidOperationException("store unavailable"));

        var result = await _sut.CheckHealthAsync(new HealthCheckContext());

        Assert.Equal(HealthStatus.Unhealthy, result.Status);
        Assert.Equal("Elsa workflow store 'triggers' is not reachable.", result.Description);
        Assert.Equal("persistence", result.Data["category"]);
        Assert.Equal("triggers", result.Data["failedStore"]);
        Assert.Equal("triggers", result.Data["failedProbe"]);
        Assert.Equal("workflow-definitions,workflow-instances,triggers,bookmark-queue", result.Data["attemptedProbes"]);
        Assert.Equal("workflow-definitions,workflow-instances,bookmark-queue", result.Data["probes"]);
    }

    [Fact]
    public async Task ReturnsHealthyWithSkippedProbesWhenOptionalManagementStoresAreMissing()
    {
        _serviceProvider.GetService(typeof(IWorkflowDefinitionStore)).Returns((object?)null);
        _serviceProvider.GetService(typeof(IWorkflowInstanceStore)).Returns((object?)null);

        var result = await _sut.CheckHealthAsync(new HealthCheckContext());

        Assert.Equal(HealthStatus.Healthy, result.Status);
        Assert.Equal("persistence", result.Data["category"]);
        Assert.Equal("triggers,bookmark-queue", result.Data["probes"]);
        Assert.Equal("triggers,bookmark-queue", result.Data["attemptedProbes"]);
        Assert.Equal("workflow-definitions,workflow-instances", result.Data["skippedProbes"]);
    }

    [Fact]
    public async Task ReturnsDegradedWithSkippedProbesWhenNoStoresAreRegistered()
    {
        _serviceProvider.GetService(typeof(IWorkflowDefinitionStore)).Returns((object?)null);
        _serviceProvider.GetService(typeof(IWorkflowInstanceStore)).Returns((object?)null);
        _serviceProvider.GetService(typeof(ITriggerStore)).Returns((object?)null);
        _serviceProvider.GetService(typeof(IBookmarkQueueStore)).Returns((object?)null);

        var result = await _sut.CheckHealthAsync(new HealthCheckContext());

        Assert.Equal(HealthStatus.Degraded, result.Status);
        Assert.Equal("No Elsa workflow persistence stores are registered.", result.Description);
        Assert.Equal("persistence", result.Data["category"]);
        Assert.Equal("workflow-definitions,workflow-instances,triggers,bookmark-queue", result.Data["skippedProbes"]);
        Assert.False(result.Data.ContainsKey("probes"));
    }
}
