using Elsa.Extensions;
using static Elsa.Workflows.Core.UnitTests.Extensions.ActivityExecutionContextExtensions.TestHelpers;

namespace Elsa.Workflows.Core.UnitTests.Extensions.ActivityExecutionContextExtensions;

public class FaultHandlingTests
{
    [Fact]
    public async Task Fault_SetsExceptionAndStatus()
    {
        // Arrange
        var context = await CreateContextAsync();
        var exception = new InvalidOperationException("Test error");

        // Act
        context.Fault(exception);

        // Assert
        Assert.Equal(ActivityStatus.Faulted, context.Status);
        Assert.Equal(exception, context.Exception);
        Assert.Equal(1, context.AggregateFaultCount);
    }

    [Fact]
    public async Task RecoverFromFault_ResetsFaultCount()
    {
        // Arrange
        var context = await CreateContextAsync();
        var exception = new InvalidOperationException("Test error");
        context.Fault(exception);

        // Act
        context.RecoverFromFault();

        // Assert
        Assert.Equal(0, context.AggregateFaultCount);
        Assert.Equal(ActivityStatus.Running, context.Status);
    }

    [Fact]
    public async Task Fault_IncrementsFaultCountOnEveryAncestor()
    {
        // Arrange
        var chain = await CreateContextChainAsync();

        // Act
        chain[^1].Fault(new InvalidOperationException("Test error"));

        // Assert
        Assert.Equal(new[] { 1, 1, 1 }, chain.Select(x => x.AggregateFaultCount));
    }

    [Fact]
    public async Task RecoverFromFault_ResetsFaultCountOnEveryAncestor()
    {
        // Arrange
        var chain = await CreateContextChainAsync();
        var faultedContext = chain[^1];
        faultedContext.Fault(new InvalidOperationException("Test error"));

        // Act
        faultedContext.RecoverFromFault();

        // Assert
        Assert.Equal(new[] { 0, 0, 0 }, chain.Select(x => x.AggregateFaultCount));
        Assert.Equal(ActivityStatus.Running, faultedContext.Status);
    }

    [Fact]
    public async Task RecoverFromFault_CalledTwice_DrivesAncestorFaultCountsNegative()
    {
        // This is why a FaultSignal handler must not call RecoverFromFault: recovery *sets* the faulting context's
        // count to zero, which is idempotent, but *decrements* every ancestor, which is not. The failure mode is
        // silently wrong fault numbers rather than an exception, so it is asserted here rather than left accidental.

        // Arrange
        var chain = await CreateContextChainAsync();
        var faultedContext = chain[^1];
        faultedContext.Fault(new InvalidOperationException("Test error"));

        // Act
        faultedContext.RecoverFromFault();
        faultedContext.RecoverFromFault();

        // Assert
        Assert.Equal(0, faultedContext.AggregateFaultCount);
        Assert.Equal(new[] { -1, -1 }, chain.Take(chain.Count - 1).Select(x => x.AggregateFaultCount));
    }

    [Fact]
    public async Task RecoverFromFault_LeavesAlreadyTerminalizedStatusAlone()
    {
        // A FaultSignal handler terminalizes the faulted activity, and the middleware recovers the fault bookkeeping
        // afterwards. Recovery must restore the counts without resurrecting the status the handler chose.

        // Arrange
        var chain = await CreateContextChainAsync();
        var faultedContext = chain[^1];
        faultedContext.Fault(new InvalidOperationException("Test error"));
        faultedContext.TransitionTo(ActivityStatus.Canceled);

        // Act
        faultedContext.RecoverFromFault();

        // Assert
        Assert.Equal(ActivityStatus.Canceled, faultedContext.Status);
        Assert.Equal(new[] { 0, 0, 0 }, chain.Select(x => x.AggregateFaultCount));
    }

    [Fact]
    public async Task Fault_RecordsAnIncident()
    {
        // Arrange
        var context = await CreateContextAsync();

        // Act
        context.Fault(new InvalidOperationException("Test error"));

        // Assert
        var incident = Assert.Single(context.WorkflowExecutionContext.Incidents);
        Assert.Equal(context.NodeId, incident.ActivityNodeId);
        Assert.Equal("Test error", incident.Message);
    }

    [Fact]
    public async Task RecoverFromFault_RemovesTheIncidentAndTheException()
    {
        // A fault an enclosing container claimed is not an incident. Plenty of code reads
        // WorkflowExecutionContext.Incidents as "this workflow failed" without looking further - the HTTP endpoint
        // fault handler among them - and would otherwise answer a caller with a fault response for a workflow that
        // caught its error and completed normally.

        // Arrange
        var context = await CreateContextAsync();
        context.Fault(new InvalidOperationException("Test error"));

        // Act
        context.RecoverFromFault();

        // Assert
        Assert.Empty(context.WorkflowExecutionContext.Incidents);
        Assert.Null(context.Exception);
    }

    [Fact]
    public async Task RecoverFromFault_LeavesIncidentsFromAnotherExecutionOfTheSameNode()
    {
        // ActivityNodeId identifies the static workflow node, and one node can have several executions: inside a loop,
        // retried, or run concurrently. Matching an incident on the node id alone would let one execution's recovery
        // remove another execution's incident, so the match is on the execution id.

        // Arrange: two executions of one activity, hence one shared node id and two distinct execution ids. The ids are
        // assigned here because this fixture substitutes the identity generator, which hands every context an empty id.
        var first = await CreateContextAsync();
        var second = await first.WorkflowExecutionContext.CreateActivityExecutionContextAsync(first.Activity, new());
        first.Id = "execution-1";
        second.Id = "execution-2";

        Assert.Equal(first.NodeId, second.NodeId);

        first.Fault(new InvalidOperationException("First execution"));
        second.Fault(new InvalidOperationException("Second execution"));

        // Act: recover the earlier execution, whose incident is not the most recently appended.
        first.RecoverFromFault();

        // Assert: the other execution keeps its own.
        var remaining = Assert.Single(first.WorkflowExecutionContext.Incidents);
        Assert.Equal(second.Id, remaining.ActivityInstanceId);
        Assert.Equal("Second execution", remaining.Message);
    }

    [Fact]
    public async Task Fault_StampsTheIncidentWithTheExecutionThatRaisedIt()
    {
        // Arrange
        var context = await CreateContextAsync();

        // Act
        context.Fault(new InvalidOperationException("Test error"));

        // Assert
        var incident = Assert.Single(context.WorkflowExecutionContext.Incidents);
        Assert.Equal(context.Id, incident.ActivityInstanceId);
    }

    [Fact]
    public async Task RecoverFromFault_LeavesIncidentsBelongingToOtherActivities()
    {
        // Arrange
        var chain = await CreateContextChainAsync();
        var other = chain[0];
        var faultedContext = chain[^1];
        other.Fault(new InvalidOperationException("Someone else's problem"));
        faultedContext.Fault(new InvalidOperationException("Test error"));

        // Act
        faultedContext.RecoverFromFault();

        // Assert
        var remaining = Assert.Single(faultedContext.WorkflowExecutionContext.Incidents);
        Assert.Equal(other.NodeId, remaining.ActivityNodeId);
    }

    [Fact]
    public async Task RecoverFromFault_RemovesOneIncidentPerFault()
    {
        // An activity that faults, is recovered, and faults again keeps the incident that was never recovered.
        // Recovery pairs with a single fault rather than wiping the activity's history wholesale.

        // Arrange
        var context = await CreateContextAsync();
        context.Fault(new InvalidOperationException("First"));
        context.RecoverFromFault();
        context.Fault(new InvalidOperationException("Second"));

        // Assert
        var incident = Assert.Single(context.WorkflowExecutionContext.Incidents);
        Assert.Equal("Second", incident.Message);
    }

    [Fact]
    public async Task RecoverFromFault_WithNoIncidentIsHarmless()
    {
        // Arrange
        var context = await CreateContextAsync();

        // Act
        context.RecoverFromFault();

        // Assert
        Assert.Empty(context.WorkflowExecutionContext.Incidents);
        Assert.Null(context.Exception);
    }
}
