using Elsa.Workflows.ComponentTests.Abstractions;
using Elsa.Workflows.ComponentTests.Fixtures;

namespace Elsa.Workflows.ComponentTests.Scenarios.Activities.Primitives.Complete;

public class CompleteActivityTests(App app) : AppComponentTest(app)
{
    [Theory(DisplayName = "Complete activity should finish workflow")]
    [InlineData(nameof(CompleteWorkflow))]
    [InlineData(nameof(CompleteWithOutputWorkflow))]
    public async Task Complete_ShouldFinishWorkflow(string workflowName)
    {
        // Arrange
        var definitionId = GetDefinitionId(workflowName);
        
        // Act
        var result = await RunWorkflowAsync(definitionId);

        // Assert
        Assert.Equal(WorkflowSubStatus.Finished, result.WorkflowExecutionContext.SubStatus);
        Assert.Equal(WorkflowStatus.Finished, result.WorkflowExecutionContext.Status);
    }

    [Fact(DisplayName = "Complete activity should stop execution of subsequent activities")]
    public async Task Complete_ShouldStopSubsequentActivities()
    {
        var result = await RunWorkflowAsync(CompleteWorkflow.DefinitionId);
        
        var messages = GetWriteLineMessages(result);
        
        // Only the first WriteLine should execute, the second one after Complete should not
        Assert.Single(messages);
        Assert.Equal("Before complete", messages[0]);
    }

    private static string GetDefinitionId(string workflowName) => workflowName switch
    {
        nameof(CompleteWorkflow) => CompleteWorkflow.DefinitionId,
        nameof(CompleteWithOutputWorkflow) => CompleteWithOutputWorkflow.DefinitionId,
        _ => throw new ArgumentException($"Unknown workflow: {workflowName}")
    };
}


