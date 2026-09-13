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

    [Test]
    public async Task ReturnsHealthyWhenAllStoresCanBeRead()
    {
        var result = await _sut.CheckHealthAsync(new HealthCheckContext());

        await Assert.That(result.Status).IsEqualTo(HealthStatus.Healthy);
        await Assert.That(result.Data["category"]).IsEqualTo("persistence");
        await Assert.That(result.Data["successfulProbes"]).IsEqualTo("workflow-definitions,workflow-instances,triggers,bookmark-queue");
        await Assert.That(result.Data["attemptedProbes"]).IsEqualTo("workflow-definitions,workflow-instances,triggers,bookmark-queue");
        await _workflowDefinitionStore.Received(1).FindAsync(
            Arg.Is<WorkflowDefinitionFilter>(x => x.Id == "00000000-0000-0000-0000-000000000000"),
            Arg.Any<CancellationToken>());
    }

    [Test]
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

        await Assert.That(result.Status).IsEqualTo(HealthStatus.Healthy);
        await Assert.That(tracker.MaxConcurrentProbes).IsEqualTo(1);
    }

    [Test]
    public async Task ReturnsUnhealthyWithFailedStoreAndStopsProbingWhenAStoreProbeFails()
    {
        _triggerStore.FindAsync(Arg.Any<TriggerFilter>(), Arg.Any<CancellationToken>()).Returns<ValueTask<Elsa.Workflows.Runtime.Entities.StoredTrigger?>>(_ => throw new InvalidOperationException("store unavailable"));

        var result = await _sut.CheckHealthAsync(new HealthCheckContext());

        await Assert.That(result.Status).IsEqualTo(HealthStatus.Unhealthy);
        await Assert.That(result.Description).IsEqualTo("Elsa workflow store 'triggers' is not reachable.");
        await Assert.That(result.Data["category"]).IsEqualTo("persistence");
        await Assert.That(result.Data["failedStore"]).IsEqualTo("triggers");
        await Assert.That(result.Data["failedProbe"]).IsEqualTo("triggers");
        await Assert.That(result.Data["successfulProbes"]).IsEqualTo("workflow-definitions,workflow-instances");
        await Assert.That(result.Data["attemptedProbes"]).IsEqualTo("workflow-definitions,workflow-instances,triggers");
        await Assert.That(result.Data["failedProbes"]).IsEqualTo("triggers");
        await _bookmarkQueueStore.DidNotReceive().FindAsync(Arg.Any<BookmarkQueueFilter>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task ReturnsUnhealthyWithAllProbeDataWhenContinuationIsEnabled()
    {
        var sut = CreateSut(continueAfterFailure: true);
        _triggerStore.FindAsync(Arg.Any<TriggerFilter>(), Arg.Any<CancellationToken>()).Returns<ValueTask<Elsa.Workflows.Runtime.Entities.StoredTrigger?>>(_ => throw new InvalidOperationException("store unavailable"));

        var result = await sut.CheckHealthAsync(new HealthCheckContext());

        await Assert.That(result.Status).IsEqualTo(HealthStatus.Unhealthy);
        await Assert.That(result.Data["successfulProbes"]).IsEqualTo("workflow-definitions,workflow-instances,bookmark-queue");
        await Assert.That(result.Data["attemptedProbes"]).IsEqualTo("workflow-definitions,workflow-instances,triggers,bookmark-queue");
        await Assert.That(result.Data["failedProbes"]).IsEqualTo("triggers");
        await _bookmarkQueueStore.Received(1).FindAsync(Arg.Any<BookmarkQueueFilter>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task ReturnsHealthyWithSkippedProbesWhenOptionalManagementStoresAreMissing()
    {
        _serviceProvider.GetService(typeof(IWorkflowDefinitionStore)).Returns((object?)null);
        _serviceProvider.GetService(typeof(IWorkflowInstanceStore)).Returns((object?)null);

        var result = await _sut.CheckHealthAsync(new HealthCheckContext());

        await Assert.That(result.Status).IsEqualTo(HealthStatus.Healthy);
        await Assert.That(result.Data["category"]).IsEqualTo("persistence");
        await Assert.That(result.Data["successfulProbes"]).IsEqualTo("triggers,bookmark-queue");
        await Assert.That(result.Data["attemptedProbes"]).IsEqualTo("triggers,bookmark-queue");
        await Assert.That(result.Data["skippedProbes"]).IsEqualTo("workflow-definitions,workflow-instances");
    }

    [Test]
    public async Task ReturnsDegradedWithSkippedProbesWhenNoStoresAreRegistered()
    {
        _serviceProvider.GetService(typeof(IWorkflowDefinitionStore)).Returns((object?)null);
        _serviceProvider.GetService(typeof(IWorkflowInstanceStore)).Returns((object?)null);
        _serviceProvider.GetService(typeof(ITriggerStore)).Returns((object?)null);
        _serviceProvider.GetService(typeof(IBookmarkQueueStore)).Returns((object?)null);

        var result = await _sut.CheckHealthAsync(new HealthCheckContext());

        await Assert.That(result.Status).IsEqualTo(HealthStatus.Degraded);
        await Assert.That(result.Description).IsEqualTo("No Elsa workflow persistence stores are registered.");
        await Assert.That(result.Data["category"]).IsEqualTo("persistence");
        await Assert.That(result.Data["skippedProbes"]).IsEqualTo("workflow-definitions,workflow-instances,triggers,bookmark-queue");
        await Assert.That(result.Data.ContainsKey("successfulProbes")).IsFalse();
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
