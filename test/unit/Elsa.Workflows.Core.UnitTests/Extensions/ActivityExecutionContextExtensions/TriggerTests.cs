using Elsa.Extensions;
using Elsa.Testing.Shared;
using Elsa.Workflows;
using Elsa.Workflows.Activities;
using static Elsa.Workflows.Core.UnitTests.Extensions.ActivityExecutionContextExtensions.TestHelpers;

namespace Elsa.Workflows.Core.UnitTests.Extensions.ActivityExecutionContextExtensions;

public class TriggerTests
{

    [Theory]
    [InlineData(true, null)] // null means use context.Activity.Id
    [InlineData(false, "different-id")]
    public async Task IsTriggerOfWorkflow_ReturnsExpectedResult(bool expectedResult, string? triggerActivityId)
    {
        // Arrange
        var context = await CreateContextAsync();
        context.WorkflowExecutionContext.TriggerActivityId = triggerActivityId ?? context.Activity.Id;

        // Act
        var result = context.IsTriggerOfWorkflow();

        // Assert
        Assert.Equal(expectedResult, result);
    }
}
