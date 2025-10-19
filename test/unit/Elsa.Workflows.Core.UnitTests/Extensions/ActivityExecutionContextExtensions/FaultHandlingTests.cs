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
}
