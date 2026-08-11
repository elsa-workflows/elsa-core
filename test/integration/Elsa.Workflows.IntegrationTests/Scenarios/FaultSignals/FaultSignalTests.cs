using Elsa.Extensions;
using Elsa.Testing.Shared;
using Elsa.Testing.Shared.Activities;
using Elsa.Workflows.Activities;
using Elsa.Workflows.IncidentStrategies;
using Elsa.Workflows.IntegrationTests.Scenarios.FaultSignals.Activities;
using Elsa.Workflows.Models;
using Elsa.Workflows.Signals;
using Xunit.Abstractions;

namespace Elsa.Workflows.IntegrationTests.Scenarios.FaultSignals;

/// <summary>
/// Integration tests for <see cref="FaultSignal"/>: a container's opportunity to claim a child's fault before it
/// becomes a workflow-global incident.
/// </summary>
public class FaultSignalTests(ITestOutputHelper testOutputHelper)
{
    private readonly WorkflowTestFixture _fixture = new(testOutputHelper);
    private readonly Fault _faultingActivity = Fault.Create("Whoops!", "Test", "Test");

    [Fact(DisplayName = "A container that handles the signal suppresses the incident strategy")]
    public async Task HandledFault_DoesNotFaultTheWorkflow()
    {
        // Arrange
        var container = ContainerAround(_faultingActivity, ClaimAndCancelChildAsync);

        // Act
        var result = await RunAsync(container);

        // Assert
        Assert.Equal(1, container.FaultsSeen);
        Assert.Equal(WorkflowStatus.Finished, result.WorkflowState.Status);
        Assert.Equal(WorkflowSubStatus.Finished, result.WorkflowState.SubStatus);

        // The incident stays on record even though the fault was handled: caught failures remain visible to operators.
        Assert.Single(result.WorkflowState.Incidents);
    }

    [Theory(DisplayName = "A fault nobody handles is left to the incident strategy, exactly as before")]
    [InlineData(typeof(FaultStrategy), WorkflowSubStatus.Faulted)]
    [InlineData(typeof(ContinueWithIncidentsStrategy), WorkflowSubStatus.Suspended)]
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
        Assert.Equal(expectedSubStatus, result.WorkflowState.SubStatus);
        Assert.Single(result.WorkflowState.Incidents);
        Assert.Equal(ActivityStatus.Faulted, result.GetActivityStatus(_faultingActivity));
        AssertFaultCounts(result, expected: 1);
    }

    [Fact(DisplayName = "A fault the inner container declines keeps bubbling to the outer one")]
    public async Task DeclinedFault_ReachesTheNextAncestor()
    {
        // Arrange
        var inner = ContainerAround(_faultingActivity);
        var outer = ContainerAround(inner, ClaimAndCancelChildAsync);

        // Act
        var result = await RunAsync(outer);

        // Assert
        Assert.Equal(1, inner.FaultsSeen);
        Assert.Equal(1, outer.FaultsSeen);
        Assert.Equal(WorkflowSubStatus.Finished, result.WorkflowState.SubStatus);
    }

    [Fact(DisplayName = "The faulting activity receives its own fault before any ancestor does")]
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
        Assert.Equal(0, container.FaultsSeen);
        Assert.NotEqual(WorkflowSubStatus.Faulted, result.WorkflowState.SubStatus);

        // The incident is still on record, and the fault bookkeeping was still recovered exactly once.
        Assert.Single(result.WorkflowState.Incidents);

        var faultedContext = result.GetActivityContext(faultingActivity);
        Assert.NotNull(faultedContext);
        Assert.Equal(0, faultedContext.AggregateFaultCount);
        Assert.All(faultedContext.GetAncestors(), x => Assert.Equal(0, x.AggregateFaultCount));
    }

    [Fact(DisplayName = "A handled fault restores the fault count on the faulting context and every ancestor")]
    public async Task HandledFault_RestoresFaultCounts()
    {
        // Arrange
        var container = ContainerAround(_faultingActivity, ClaimAndCancelChildAsync);

        // Act
        var result = await RunAsync(container);

        // Assert
        AssertFaultCounts(result, expected: 0);
    }

    [Fact(DisplayName = "A handler that also recovers from the fault drives ancestor fault counts negative")]
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
        var faultedContext = result.GetActivityContext(_faultingActivity);
        Assert.NotNull(faultedContext);

        var ancestors = faultedContext.GetAncestors().ToList();
        Assert.NotEmpty(ancestors);
        Assert.Equal(0, faultedContext.AggregateFaultCount);
        Assert.All(ancestors, x => Assert.Equal(-1, x.AggregateFaultCount));
    }

    [Fact(DisplayName = "A handler that cancels the faulted child leaves it Canceled, not Running")]
    public async Task HandlerThatCancelsChild_LeavesItCanceled()
    {
        // Arrange
        var container = ContainerAround(_faultingActivity, ClaimAndCancelChildAsync);

        // Act
        var result = await RunAsync(container);

        // Assert
        Assert.Equal(ActivityStatus.Canceled, result.GetActivityStatus(_faultingActivity));
    }

    [Fact(DisplayName = "A handler that terminalizes nothing leaves the child Running and the workflow suspended")]
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
        Assert.Equal(ActivityStatus.Running, result.GetActivityStatus(_faultingActivity));
        Assert.Equal(WorkflowStatus.Running, result.WorkflowState.Status);
        Assert.Equal(WorkflowSubStatus.Suspended, result.WorkflowState.SubStatus);
    }

    [Fact(DisplayName = "A completing container sweeps a child the handler failed to terminalize")]
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
        Assert.Equal(ActivityStatus.Completed, result.GetActivityStatus(container));
        Assert.Equal(ActivityStatus.Canceled, result.GetActivityStatus(_faultingActivity));
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

    private void AssertFaultCounts(RunWorkflowResult result, int expected)
    {
        var faultedContext = result.GetActivityContext(_faultingActivity);
        Assert.NotNull(faultedContext);

        var ancestors = faultedContext.GetAncestors().ToList();
        Assert.NotEmpty(ancestors);
        Assert.Equal(expected, faultedContext.AggregateFaultCount);
        Assert.All(ancestors, x => Assert.Equal(expected, x.AggregateFaultCount));
    }
}
