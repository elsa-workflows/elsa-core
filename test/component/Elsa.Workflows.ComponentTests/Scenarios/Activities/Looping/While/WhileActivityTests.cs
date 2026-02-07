using Elsa.Workflows.ComponentTests.Abstractions;
using Elsa.Workflows.ComponentTests.Fixtures;

namespace Elsa.Workflows.ComponentTests.Scenarios.Activities.Looping.While;

public class WhileActivityTests(App app) : AppComponentTest(app)
{
    [Theory(DisplayName = "While activity should execute correct number of iterations")]
    [InlineData(nameof(WhileCounterWorkflow), 6)]  // 5 iterations + final message
    [InlineData(nameof(WhileFalseConditionWorkflow), 1)]  // Only final message
    [InlineData(nameof(WhileWithBreakWorkflow), 4)]  // 3 iterations + final message (breaks at 3)
    public async Task While_ShouldExecuteCorrectIterations(string workflowName, int expectedWriteLineCount)
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

    [Fact(DisplayName = "While with false condition should only execute final message")]
    public async Task While_WhenConditionIsFalse_ShouldOnlyExecuteFinalMessage()
    {
        var result = await RunWorkflowAsync(WhileFalseConditionWorkflow.DefinitionId);
        
        AssertWorkflowFinished(result);
        var messages = GetWriteLineMessages(result);
        
        Assert.Single(messages);
        Assert.Equal("Loop completed", messages[0]);
    }

    private static string GetDefinitionId(string workflowName) => workflowName switch
    {
        nameof(WhileCounterWorkflow) => WhileCounterWorkflow.DefinitionId,
        nameof(WhileFalseConditionWorkflow) => WhileFalseConditionWorkflow.DefinitionId,
        nameof(WhileWithBreakWorkflow) => WhileWithBreakWorkflow.DefinitionId,
        _ => throw new ArgumentException($"Unknown workflow: {workflowName}")
    };
}

