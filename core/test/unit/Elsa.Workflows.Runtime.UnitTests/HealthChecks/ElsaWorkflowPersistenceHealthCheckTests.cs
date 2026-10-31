using Elsa.Workflows.Management;
using Elsa.Workflows.Management.Filters;
using Elsa.Workflows.Runtime.Filters;
using Elsa.Workflows.Runtime.HealthChecks;
using Elsa.Workflows.Runtime.Options;
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
        _sut = CreateSut();
    }

    [Fact]
    public async Task ReturnsHealthyWhenAllStoresCanBeRead()
    {
        var result = await _sut.CheckHealthAsync(new HealthCheckContext());

        Assert.Equal(HealthStatus.Healthy, result.Status);
        Assert.Equal("persistence", result.Data["category"]);
        Assert.Equal("workflow-definitions,workflow-instances,triggers,bookmark-queue", result.Data["successfulProbes"]);
        Assert.Equal("workflow-definitions,workflow-instances,triggers,bookmark-queue", result.Data["attemptedProbes"]);
        await _workflowDefinitionStore.Received(1).FindAsync(
            Arg.Is<WorkflowDefinitionFilter>(x => x.Id == "00000000-0000-0000-0000-000000000000"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProbesStoresSequentially()
    {
        var tracker = new ProbeConcurrencyTracker();
        _workflowDefinitionStore.FindAsync(Arg.Any<WorkflowDefinitionFilter>(), Arg.Any<CancellationToken>())
            .Returns(_ => TrackProbeAsync<Elsa.Workflows.Management.Entities.WorkflowDefinition?>(tracker, null));
        _workflowInstanceStore.CountAsync(Arg.Any<WorkflowInstanceFilter>(), Arg.Any<CancellationToken>())
            .Returns(_ => new ValueTask<long>(TrackProbeAsync(tracker, 0L)));
        _triggerStore.FindAsync(Arg.Any<TriggerFilter>(), Arg.Any<CancellationToken>())
            .Returns(_ => new ValueTask<Elsa.Workflows.Runtime.Entities.StoredTrigger?>(TrackProbeAsync<Elsa.Workflows.Runtime.Entities.StoredTrigger?>(tracker, null)));
        _bookmarkQueueStore.FindAsync(Arg.Any<BookmarkQueueFilter>(), Arg.Any<CancellationToken>())
            .Returns(_ => TrackProbeAsync<Elsa.Workflows.Runtime.Entities.BookmarkQueueItem?>(tracker, null));

        var result = await _sut.CheckHealthAsync(new HealthCheckContext());

        Assert.Equal(HealthStatus.Healthy, result.Status);
        Assert.Equal(1, tracker.MaxConcurrentProbes);
    }

    [Fact]
    public async Task ReturnsUnhealthyWithFailedStoreAndStopsProbingWhenAStoreProbeFails()
    {
        _triggerStore.FindAsync(Arg.Any<TriggerFilter>(), Arg.Any<CancellationToken>()).Returns<ValueTask<Elsa.Workflows.Runtime.Entities.StoredTrigger?>>(_ => throw new InvalidOperationException("store unavailable"));

        var result = await _sut.CheckHealthAsync(new HealthCheckContext());

        Assert.Equal(HealthStatus.Unhealthy, result.Status);
        Assert.Equal("Elsa workflow store 'triggers' is not reachable.", result.Description);
        Assert.Equal("persistence", result.Data["category"]);
        Assert.Equal("triggers", result.Data["failedStore"]);
        Assert.Equal("triggers", result.Data["failedProbe"]);
        Assert.Equal("workflow-definitions,workflow-instances", result.Data["successfulProbes"]);
        Assert.Equal("workflow-definitions,workflow-instances,triggers", result.Data["attemptedProbes"]);
        Assert.Equal("triggers", result.Data["failedProbes"]);
        await _bookmarkQueueStore.DidNotReceive().FindAsync(Arg.Any<BookmarkQueueFilter>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ReturnsUnhealthyWithAllProbeDataWhenContinuationIsEnabled()
    {
        var sut = CreateSut(continueAfterFailure: true);
        _triggerStore.FindAsync(Arg.Any<TriggerFilter>(), Arg.Any<CancellationToken>()).Returns<ValueTask<Elsa.Workflows.Runtime.Entities.StoredTrigger?>>(_ => throw new InvalidOperationException("store unavailable"));

        var result = await sut.CheckHealthAsync(new HealthCheckContext());

        Assert.Equal(HealthStatus.Unhealthy, result.Status);
        Assert.Equal("workflow-definitions,workflow-instances,bookmark-queue", result.Data["successfulProbes"]);
        Assert.Equal("workflow-definitions,workflow-instances,triggers,bookmark-queue", result.Data["attemptedProbes"]);
        Assert.Equal("triggers", result.Data["failedProbes"]);
        await _bookmarkQueueStore.Received(1).FindAsync(Arg.Any<BookmarkQueueFilter>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ReturnsHealthyWithSkippedProbesWhenOptionalManagementStoresAreMissing()
    {
        _serviceProvider.GetService(typeof(IWorkflowDefinitionStore)).Returns((object?)null);
        _serviceProvider.GetService(typeof(IWorkflowInstanceStore)).Returns((object?)null);

        var result = await _sut.CheckHealthAsync(new HealthCheckContext());

        Assert.Equal(HealthStatus.Healthy, result.Status);
        Assert.Equal("persistence", result.Data["category"]);
        Assert.Equal("triggers,bookmark-queue", result.Data["successfulProbes"]);
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
        Assert.False(result.Data.ContainsKey("successfulProbes"));
    }

    private ElsaWorkflowPersistenceHealthCheck CreateSut(bool continueAfterFailure = false)
    {
        var options = Microsoft.Extensions.Options.Options.Create(new ElsaReadinessHealthCheckOptions
        {
            ContinuePersistenceProbesAfterFailure = continueAfterFailure
        });

        return new ElsaWorkflowPersistenceHealthCheck(_serviceProvider, options, NullLogger<ElsaWorkflowPersistenceHealthCheck>.Instance);
    }

    private static async Task<T> TrackProbeAsync<T>(ProbeConcurrencyTracker tracker, T result)
    {
        tracker.Enter();

        try
        {
            await Task.Delay(10);
            return result;
        }
        finally
        {
            tracker.Exit();
        }
    }

    private sealed class ProbeConcurrencyTracker
    {
        private int _currentProbes;
        private int _maxConcurrentProbes;

        public int MaxConcurrentProbes => Volatile.Read(ref _maxConcurrentProbes);

        public void Enter()
        {
            var currentProbes = Interlocked.Increment(ref _currentProbes);

            while (true)
            {
                var maxConcurrentProbes = MaxConcurrentProbes;
                if (currentProbes <= maxConcurrentProbes)
                    return;

                if (Interlocked.CompareExchange(ref _maxConcurrentProbes, currentProbes, maxConcurrentProbes) == maxConcurrentProbes)
                    return;
            }
        }

        public void Exit() => Interlocked.Decrement(ref _currentProbes);
    }
}
