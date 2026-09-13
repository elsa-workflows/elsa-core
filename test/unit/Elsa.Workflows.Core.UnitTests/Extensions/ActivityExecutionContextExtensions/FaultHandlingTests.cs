using Elsa.Extensions;
using static Elsa.Workflows.Core.UnitTests.Extensions.ActivityExecutionContextExtensions.TestHelpers;
using System.Threading.Tasks;

namespace Elsa.Workflows.Core.UnitTests.Extensions.ActivityExecutionContextExtensions;

public class FaultHandlingTests
{
    [Test]
    public async Task Fault_SetsExceptionAndStatus()
    {
        // Arrange
        var context = await CreateContextAsync();
        var exception = new InvalidOperationException("Test error");

        // Act
        context.Fault(exception);

        // Assert
        await Assert.That(context.Status).IsEqualTo(ActivityStatus.Faulted);
        await Assert.That(context.Exception).IsEqualTo(exception);
        await Assert.That(context.AggregateFaultCount).IsEqualTo(1);
    }

    [Test]
    public async Task RecoverFromFault_ResetsFaultCount()
    {
        // Arrange
        var context = await CreateContextAsync();
        var exception = new InvalidOperationException("Test error");
        context.Fault(exception);

        // Act
        context.RecoverFromFault();

        // Assert
        await Assert.That(context.AggregateFaultCount).IsEqualTo(0);
        await Assert.That(context.Status).IsEqualTo(ActivityStatus.Running);
    }

    [Test]
    public async Task Fault_IncrementsFaultCountOnEveryAncestor()
    {
        // Arrange
        var chain = await CreateContextChainAsync();

        // Act
        chain[^1].Fault(new InvalidOperationException("Test error"));

        // Assert
        await Assert.That(chain.Select(x => x.AggregateFaultCount))
            .IsEquivalentTo(new[] { 1, 1, 1 }, TUnit.Assertions.Enums.CollectionOrdering.Matching);
    }

    [Test]
    public async Task RecoverFromFault_ResetsFaultCountOnEveryAncestor()
    {
        // Arrange
        var chain = await CreateContextChainAsync();
        var faultedContext = chain[^1];
        faultedContext.Fault(new InvalidOperationException("Test error"));

        // Act
        faultedContext.RecoverFromFault();

        // Assert
        await Assert.That(chain.Select(x => x.AggregateFaultCount))
            .IsEquivalentTo(new[] { 0, 0, 0 }, TUnit.Assertions.Enums.CollectionOrdering.Matching);
        await Assert.That(faultedContext.Status).IsEqualTo(ActivityStatus.Running);
    }

    [Test]
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
        await Assert.That(faultedContext.AggregateFaultCount).IsEqualTo(0);
        await Assert.That(chain.Take(chain.Count - 1).Select(x => x.AggregateFaultCount))
            .IsEquivalentTo(new[] { -1, -1 }, TUnit.Assertions.Enums.CollectionOrdering.Matching);
    }

    [Test]
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
        await Assert.That(faultedContext.Status).IsEqualTo(ActivityStatus.Canceled);
        await Assert.That(chain.Select(x => x.AggregateFaultCount))
            .IsEquivalentTo(new[] { 0, 0, 0 }, TUnit.Assertions.Enums.CollectionOrdering.Matching);
    }

    [Test]
    public async Task Fault_RecordsAnIncident()
    {
        // Arrange
        var context = await CreateContextAsync();

        // Act
        context.Fault(new InvalidOperationException("Test error"));

        // Assert
        var incident = await Assert.That(context.WorkflowExecutionContext.Incidents).HasSingleItem();
        await Assert.That(incident.ActivityNodeId).IsEqualTo(context.NodeId);
        await Assert.That(incident.Message).IsEqualTo("Test error");
    }

    [Test]
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
        await Assert.That(context.WorkflowExecutionContext.Incidents).IsEmpty();
        await Assert.That(context.Exception).IsNull();
    }

    [Test]
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

        await Assert.That(second.NodeId).IsEqualTo(first.NodeId);

        first.Fault(new InvalidOperationException("First execution"));
        second.Fault(new InvalidOperationException("Second execution"));

        // Act: recover the earlier execution, whose incident is not the most recently appended.
        first.RecoverFromFault();

        // Assert: the other execution keeps its own.
        var remaining = await Assert.That(first.WorkflowExecutionContext.Incidents).HasSingleItem();
        await Assert.That(remaining.ActivityInstanceId).IsEqualTo(second.Id);
        await Assert.That(remaining.Message).IsEqualTo("Second execution");
    }

    [Test]
    public async Task Fault_StampsTheIncidentWithTheExecutionThatRaisedIt()
    {
        // Arrange
        var context = await CreateContextAsync();

        // Act
        context.Fault(new InvalidOperationException("Test error"));

        // Assert
        var incident = await Assert.That(context.WorkflowExecutionContext.Incidents).HasSingleItem();
        await Assert.That(incident.ActivityInstanceId).IsEqualTo(context.Id);
    }

    [Test]
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
        var remaining = await Assert.That(faultedContext.WorkflowExecutionContext.Incidents).HasSingleItem();
        await Assert.That(remaining.ActivityNodeId).IsEqualTo(other.NodeId);
    }

    [Test]
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
        var incident = await Assert.That(context.WorkflowExecutionContext.Incidents).HasSingleItem();
        await Assert.That(incident.Message).IsEqualTo("Second");
    }

    [Test]
    public async Task RecoverFromFault_WithNoIncidentIsHarmless()
    {
        // Arrange
        var context = await CreateContextAsync();

        // Act
        context.RecoverFromFault();

        // Assert
        await Assert.That(context.WorkflowExecutionContext.Incidents).IsEmpty();
        await Assert.That(context.Exception).IsNull();
    }
}
