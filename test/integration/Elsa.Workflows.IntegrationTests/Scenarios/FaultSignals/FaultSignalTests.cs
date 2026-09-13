using Elsa.Common.Models;
using Elsa.Extensions;
using Elsa.Testing.Shared;
using Elsa.Testing.Shared.Activities;
using Elsa.Workflows.Activities;
using Elsa.Workflows.IncidentStrategies;
using Elsa.Workflows.IntegrationTests.Scenarios.FaultSignals.Activities;
using Elsa.Workflows.Models;
using Elsa.Workflows.Runtime;
using Microsoft.Extensions.DependencyInjection;
using Elsa.Workflows.Signals;

namespace Elsa.Workflows.IntegrationTests.Scenarios.FaultSignals;

/// <summary>
/// Integration tests for <see cref="FaultSignal"/>: a container's opportunity to claim a child's fault before it
/// becomes a workflow-global incident.
/// </summary>
public class FaultSignalTests : IAsyncDisposable
{
    private readonly WorkflowTestFixture _fixture = new(TestContext.Current!.Output.StandardOutput);
    private readonly Fault _faultingActivity = Fault.Create("Whoops!", "Test", "Test");

    [Test]
    [DisplayName("A container that handles the signal suppresses the incident strategy")]
    public async Task HandledFault_DoesNotFaultTheWorkflow()
    {
        // Arrange
        var container = ContainerAround(_faultingActivity, ClaimAndCancelChildAsync);

        // Act
        var result = await RunAsync(container);

        // Assert
        await Assert.That(container.FaultsSeen).IsEqualTo(1);
        await Assert.That(result.WorkflowState.Status).IsEqualTo(WorkflowStatus.Finished);
        await Assert.That(result.WorkflowState.SubStatus).IsEqualTo(WorkflowSubStatus.Finished);

        // A fault a container claimed is not an incident. Plenty of code reads a non-empty Incidents as "this workflow
        // failed" without looking further - the HTTP endpoint fault handler among them - so leaving one here would
        // answer a caller with a fault response for a workflow that caught its error and finished normally. The
        // execution log still records the failure for anyone reading the journal.
        await Assert.That(result.WorkflowState.Incidents).IsEmpty();
    }

    [Test]
    [DisplayName("A fault nobody handles is left to the incident strategy, exactly as before: $incidentStrategyType")]
    [Arguments(typeof(FaultStrategy), WorkflowSubStatus.Faulted)]
    [Arguments(typeof(ContinueWithIncidentsStrategy), WorkflowSubStatus.Suspended)]
    public async Task UnhandledFault_LeavesIncidentStrategyInCharge(Type incidentStrategyType, WorkflowSubStatus expectedSubStatus)
    {
        // Arrange: a plain container, with no FaultSignal handler anywhere in the chain.
        var container = new TestContainer
        {
            Activities =
            {
                _faultingActivity
            }
        };

        // Act
        var result = await RunAsync(container, incidentStrategyType);

        // Assert
        await Assert.That(result.WorkflowState.SubStatus).IsEqualTo(expectedSubStatus);
        await Assert.That(result.WorkflowState.Incidents).HasSingleItem();
        await Assert.That(result.GetActivityStatus(_faultingActivity)).IsEqualTo(ActivityStatus.Faulted);
        await AssertFaultCountsAsync(result, expected: 1);
    }

    [Test]
    [DisplayName("A fault the inner container declines keeps bubbling to the outer one")]
    public async Task DeclinedFault_ReachesTheNextAncestor()
    {
        // Arrange
        var inner = ContainerAround(_faultingActivity);
        var outer = ContainerAround(inner, ClaimAndCancelChildAsync);

        // Act
        var result = await RunAsync(outer);

        // Assert
        await Assert.That(inner.FaultsSeen).IsEqualTo(1);
        await Assert.That(outer.FaultsSeen).IsEqualTo(1);
        await Assert.That(result.WorkflowState.SubStatus).IsEqualTo(WorkflowSubStatus.Finished);
    }

    [Test]
    [DisplayName("The faulting activity receives its own fault before any ancestor does")]
    public async Task FaultingActivity_ReceivesItsOwnSignalFirst()
    {
        // Pinning the channel's self-plus-ancestors dispatch, which this signal reuses rather than varying. It lets a
        // self-retrying or self-compensating activity claim its own failure, and grants no ability to hide a failure
        // that an activity did not already have: one that simply catches its own exception never faults at all.

        // Arrange
        var faultingActivity = new SelfHandlingFaultingActivity();
        var container = ContainerAround(faultingActivity);

        // Act
        var result = await RunAsync(container, typeof(FaultStrategy));

        // Assert: the activity claimed its own fault, so the walk never reached the container.
        await Assert.That(container.FaultsSeen).IsEqualTo(0);
        await Assert.That(result.WorkflowState.SubStatus).IsNotEqualTo(WorkflowSubStatus.Faulted);

        // The activity claimed the fault itself, so it is not an incident either, and the fault bookkeeping was still
        // recovered exactly once.
        await Assert.That(result.WorkflowState.Incidents).IsEmpty();

        var faultedContext = await Assert.That(result.GetActivityContext(faultingActivity)).IsNotNull();
        await Assert.That(faultedContext.AggregateFaultCount).IsEqualTo(0);
        foreach (var ancestor in faultedContext.GetAncestors())
            await Assert.That(ancestor.AggregateFaultCount).IsEqualTo(0);
    }

    [Test]
    [DisplayName("A handled fault restores the fault count on the faulting context and every ancestor")]
    public async Task HandledFault_RestoresFaultCounts()
    {
        // Arrange
        var container = ContainerAround(_faultingActivity, ClaimAndCancelChildAsync);

        // Act
        var result = await RunAsync(container);

        // Assert
        await AssertFaultCountsAsync(result, expected: 0);
    }

    [Test]
    [DisplayName("A handler that also recovers from the fault drives ancestor fault counts negative")]
    public async Task HandlerThatAlsoRecoversFromFault_CorruptsAncestorFaultCounts()
    {
        // Asserting the documented consequence of violating the contract rather than leaving it accidental: recovery
        // *sets* the faulting context's count to zero, which is idempotent, but *decrements* every ancestor, which is
        // not. The middleware recovers too, so the ancestors end up one below where they started.

        // Arrange
        var container = ContainerAround(_faultingActivity, async (signal, context) =>
        {
            context.StopPropagation();
            signal.FaultedContext.RecoverFromFault();
            await context.ReceiverActivityExecutionContext.CompleteActivityAsync();
        });

        // Act
        var result = await RunAsync(container);

        // Assert
        var faultedContext = await Assert.That(result.GetActivityContext(_faultingActivity)).IsNotNull();

        var ancestors = faultedContext.GetAncestors().ToList();
        await Assert.That(ancestors).IsNotEmpty();
        await Assert.That(faultedContext.AggregateFaultCount).IsEqualTo(0);
        foreach (var ancestor in ancestors)
            await Assert.That(ancestor.AggregateFaultCount).IsEqualTo(-1);
    }

    [Test]
    [DisplayName("A handler can complete the faulted child with a substitute result")]
    public async Task HandlerThatCompletesChildWithSubstituteResult_ResumesTheContainer()
    {
        // Arrange
        var container = new FaultHandlingContainer(async (signal, context) =>
        {
            context.StopPropagation();

            // CompleteActivityAsync no-ops on a non-Running activity, and the middleware recovers only once this
            // handler has returned, so the faulted child has to be moved out of Faulted first. This is not the same as
            // RecoverFromFault, which would also rewrite the fault counts.
            signal.FaultedContext.TransitionTo(ActivityStatus.Running);
            await signal.FaultedContext.CompleteActivityAsync("substitute");
        })
        {
            Activities =
            {
                _faultingActivity,
                new WriteLine("after")
            }
        };

        // Act
        var result = await RunAsync(container);

        // Assert: completing the child fires the container's completion callback, so sequencing resumes.
        await Assert.That(result.GetActivityStatus(_faultingActivity)).IsEqualTo(ActivityStatus.Completed);
        await Assert.That(_fixture.CapturingTextWriter.Lines).IsEquivalentTo(new[] { "after" }, TUnit.Assertions.Enums.CollectionOrdering.Matching);
        await Assert.That(result.WorkflowState.SubStatus).IsEqualTo(WorkflowSubStatus.Finished);
        await AssertFaultCountsAsync(result, expected: 0);
    }

    [Test]
    [DisplayName("A handler that cancels the faulted child leaves it Canceled, not Running")]
    public async Task HandlerThatCancelsChild_LeavesItCanceled()
    {
        // Arrange
        var container = ContainerAround(_faultingActivity, ClaimAndCancelChildAsync);

        // Act
        var result = await RunAsync(container);

        // Assert
        await Assert.That(result.GetActivityStatus(_faultingActivity)).IsEqualTo(ActivityStatus.Canceled);
    }

    [Test]
    [DisplayName("A handler that terminalizes nothing leaves the child Running and the workflow suspended")]
    public async Task HandlerThatTerminalizesNothing_DegradesRatherThanHangs()
    {
        // The handler's bug, documented: recovery transitions the faulted child back to Running, and a handler that
        // schedules no follow-up work moves it no further. The run returns rather than hanging, but the workflow
        // suspends with nothing left to resume it, which is why terminalizing is the handler's responsibility.

        // Arrange
        var container = ContainerAround(_faultingActivity, (_, context) =>
        {
            context.StopPropagation();
            return default;
        });

        // Act
        var result = await RunAsync(container);

        // Assert
        await Assert.That(result.GetActivityStatus(_faultingActivity)).IsEqualTo(ActivityStatus.Running);
        await Assert.That(result.WorkflowState.Status).IsEqualTo(WorkflowStatus.Running);
        await Assert.That(result.WorkflowState.SubStatus).IsEqualTo(WorkflowSubStatus.Suspended);
    }

    [Test]
    [DisplayName("A completing container sweeps a child the handler failed to terminalize")]
    public async Task HandlerThatTerminalizesNothing_IsBackstoppedByTheCompletingContainer()
    {
        // The backstop, not the mechanism: CompleteActivityAsync cancels non-completed children on the way out.

        // Arrange
        var container = ContainerAround(_faultingActivity, async (_, context) =>
        {
            context.StopPropagation();
            await context.ReceiverActivityExecutionContext.CompleteActivityAsync();
        });

        // Act
        var result = await RunAsync(container);

        // Assert
        await Assert.That(result.GetActivityStatus(container)).IsEqualTo(ActivityStatus.Completed);
        await Assert.That(result.GetActivityStatus(_faultingActivity)).IsEqualTo(ActivityStatus.Canceled);
    }

    [Test]
    [DisplayName("A handler that throws is treated as not having handled the fault: $stopPropagationFirst")]
    [Arguments(false)]
    [Arguments(true)]
    public async Task HandlerThatThrows_FallsThroughToTheIncidentStrategy(bool stopPropagationFirst)
    {
        // The signal is sent from inside the catch that exists to stop exceptions escaping the activity pipeline, so a
        // handler's failure must not escape either: it would defeat the middleware and lose the original fault with it.
        // Both cases are covered, because a handler that already claimed the fault before throwing is the one that could
        // plausibly have been mistaken for a successful handling.

        // Arrange
        var container = ContainerAround(_faultingActivity, (_, context) =>
        {
            if (stopPropagationFirst)
                context.StopPropagation();

            throw new InvalidOperationException("The handler is broken");
        });

        // Act: this must not throw. If the handler's exception escaped, it would surface here.
        var result = await RunAsync(container, typeof(FaultStrategy));

        // Assert: the fault is unhandled, so it lands exactly where it would with no handler at all.
        await Assert.That(container.FaultsSeen).IsEqualTo(1);
        await Assert.That(result.WorkflowState.SubStatus).IsEqualTo(WorkflowSubStatus.Faulted);
        await Assert.That(result.GetActivityStatus(_faultingActivity)).IsEqualTo(ActivityStatus.Faulted);

        // The original fault is the incident, not the handler's failure: recovery never ran, so nothing removed it.
        // Asserted on identity rather than message text, because the incident carries the thrown exception's message
        // and a broken handler must not be able to substitute its own.
        var incident = await Assert.That(result.WorkflowState.Incidents).HasSingleItem();
        await Assert.That(incident.ActivityId).IsEqualTo(_faultingActivity.Id);
        await Assert.That(incident.Message).DoesNotContain("The handler is broken", StringComparison.Ordinal);
        await AssertFaultCountsAsync(result, expected: 1);
    }

    [Test]
    [DisplayName("Cancellation from a handler propagates instead of becoming an incident")]
    public async Task HandlerThatCancels_PropagatesRatherThanFaulting()
    {
        // The guard above deliberately does not cover OperationCanceledException. Cancellation means the host is tearing
        // the run down, not that the handler is broken, and this repository keeps the two apart: the workflow-level
        // exception middleware cancels and rethrows before its general catch, and WorkflowRunner declines to record
        // cancellation as the workflow's exception. Swallowing it here would turn a deliberate cancellation into a
        // faulted workflow.

        // Arrange
        var container = ContainerAround(_faultingActivity, (_, _) => throw new OperationCanceledException());

        // Act
        var result = await RunAsync(container, typeof(FaultStrategy));

        // Assert: the workflow-level middleware cancelled the run, so it ends Cancelled rather than Faulted. Swallowing
        // the cancellation here would instead hand the fault to FaultStrategy and finish Faulted.
        await Assert.That(result.WorkflowState.SubStatus).IsEqualTo(WorkflowSubStatus.Cancelled);
    }

    [Test]
    [DisplayName("A handled fault still leaves the failure in the execution log")]
    public async Task HandledFault_StillRecordsTheFailureInTheJournal()
    {
        // The whole justification for dropping the incident is that the journal keeps the evidence, so that claim is
        // asserted rather than assumed. ExecutionLogMiddleware writes the entry from a catch that rethrows, and it sits
        // inside ExceptionHandlingMiddleware in the activity pipeline, so the entry is written before the fault is ever
        // offered to an ancestor. Reordering those two would silently make a handled failure invisible.

        // Arrange
        var container = ContainerAround(_faultingActivity, ClaimAndCancelChildAsync);

        // Act
        var result = await RunAsync(container);

        // Assert: no incident, but the failure is still on the record.
        await Assert.That(result.WorkflowState.Incidents).IsEmpty();

        var journal = await _fixture.Services.GetRequiredService<IWorkflowExecutionLogStore>().FindManyAsync(new()
        {
            WorkflowInstanceId = result.WorkflowState.Id,
            ActivityId = _faultingActivity.Id,
            EventName = "Faulted"
        }, PageArgs.All);

        var entry = await Assert.That(journal.Items).HasSingleItem();
        await Assert.That(entry.ActivityId).IsEqualTo(_faultingActivity.Id);
    }

    /// <summary>
    /// The canonical handler: claim the fault, terminalize the faulted child, and wind the container up.
    /// </summary>
    private static async ValueTask ClaimAndCancelChildAsync(FaultSignal signal, SignalContext context)
    {
        context.StopPropagation();
        await signal.FaultedContext.CancelActivityAsync();
        await context.ReceiverActivityExecutionContext.CompleteActivityAsync();
    }

    private static FaultHandlingContainer ContainerAround(IActivity child, Func<FaultSignal, SignalContext, ValueTask>? onChildFaulted = null)
    {
        return new(onChildFaulted)
        {
            Activities =
            {
                child
            }
        };
    }

    private Task<RunWorkflowResult> RunAsync(IActivity root, Type? incidentStrategyType = null)
    {
        return _fixture.RunWorkflowAsync(new TestWorkflow(builder =>
        {
            builder.WorkflowOptions.IncidentStrategyType = incidentStrategyType;
            builder.Root = root;
        }));
    }

    private async Task AssertFaultCountsAsync(RunWorkflowResult result, int expected)
    {
        var faultedContext = await Assert.That(result.GetActivityContext(_faultingActivity)).IsNotNull();

        var ancestors = faultedContext.GetAncestors().ToList();
        await Assert.That(ancestors).IsNotEmpty();
        await Assert.That(faultedContext.AggregateFaultCount).IsEqualTo(expected);
        foreach (var ancestor in ancestors)
            await Assert.That(ancestor.AggregateFaultCount).IsEqualTo(expected);
    }

    public ValueTask DisposeAsync() => TestResourceDisposal.DisposeAsync(_fixture);
}
