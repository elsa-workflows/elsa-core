using Elsa.Workflows.ComponentTests.Abstractions;
using Elsa.Workflows.ComponentTests.Fixtures;

namespace Elsa.Workflows.ComponentTests.Scenarios.Activities.Looping.For;

public class ForActivityTests(App app) : AppComponentTest(app)
{
    [Theory(DisplayName = "For activity should execute correct number of iterations")]
    [InlineData(nameof(ForBasicWorkflow), 6)]  // 5 iterations (0-4) + final message
    [InlineData(nameof(ForWithStepWorkflow), 4)]  // 3 iterations (0, 2, 4) + final message
    [InlineData(nameof(ForZeroIterationsWorkflow), 1)]  // Only final message
    public async Task For_ShouldExecuteCorrectIterations(string workflowName, int expectedWriteLineCount)
    {
        // Arrange
        var definitionId = GetDefinitionId(workflowName);
        
        // Act
        var result = await RunWorkflowAsync(definitionId);
        
        // Assert
        AssertWorkflowFinished(result);
        var writeLineRecords = GetWriteLineRecords(result);
        Assert.Equal(expectedWriteLineCount, writeLineRecords.Count);
    }

    [Fact(DisplayName = "For with zero iterations should only execute final message")]
    public async Task For_WhenStartEqualsEnd_ShouldOnlyExecuteFinalMessage()
    {
        var result = await RunWorkflowAsync(ForZeroIterationsWorkflow.DefinitionId);
        
        AssertWorkflowFinished(result);
        var messages = GetWriteLineMessages(result);
        
        Assert.Single(messages);
        Assert.Equal("Loop completed", messages[0]);
    }

    private static string GetDefinitionId(string workflowName) => workflowName switch
    {
        nameof(ForBasicWorkflow) => ForBasicWorkflow.DefinitionId,
        nameof(ForWithStepWorkflow) => ForWithStepWorkflow.DefinitionId,
        nameof(ForZeroIterationsWorkflow) => ForZeroIterationsWorkflow.DefinitionId,
        _ => throw new ArgumentException($"Unknown workflow: {workflowName}")
    };
}

