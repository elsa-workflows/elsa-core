using Elsa.Workflows.Activities;
using Elsa.Workflows.ComponentTests.Abstractions;
using Elsa.Workflows.ComponentTests.Fixtures;

namespace Elsa.Workflows.ComponentTests.Scenarios.Activities.Branching.If;

public class IfActivityTests(App app) : AppComponentTest(app)
{
    [Theory(DisplayName = "If activity should execute correct branch based on condition")]
    [InlineData(nameof(IfTrueWorkflow), "Condition is true", 1)]
    [InlineData(nameof(IfFalseWorkflow), "Condition is false", 1)]
    [InlineData(nameof(IfNoElseWorkflow), null, 0)]
    public async Task If_ShouldExecuteCorrectBranch(string workflowName, string? expectedMessage, int expectedWriteLineCount)
    {
        // Arrange
        var definitionId = GetDefinitionId(workflowName);
        
        // Act
        var result = await RunWorkflowAsync(definitionId);
        
        // Assert
        AssertWorkflowFinished(result);
        var writeLineRecords = GetWriteLineRecords(result);
        
        Assert.Equal(expectedWriteLineCount, writeLineRecords.Count);
        if (expectedMessage != null)
        {
            Assert.Equal(expectedMessage, writeLineRecords[0].ActivityState?[nameof(WriteLine.Text)]);
        }
    }

    private static string GetDefinitionId(string workflowName) => workflowName switch
    {
        nameof(IfTrueWorkflow) => IfTrueWorkflow.DefinitionId,
        nameof(IfFalseWorkflow) => IfFalseWorkflow.DefinitionId,
        nameof(IfNoElseWorkflow) => IfNoElseWorkflow.DefinitionId,
        _ => throw new ArgumentException($"Unknown workflow: {workflowName}")
    };
}

