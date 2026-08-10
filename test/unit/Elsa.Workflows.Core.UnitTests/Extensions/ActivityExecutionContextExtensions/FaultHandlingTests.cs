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
}
