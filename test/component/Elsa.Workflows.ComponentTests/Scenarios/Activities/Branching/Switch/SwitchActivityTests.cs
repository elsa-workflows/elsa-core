using Elsa.Workflows.ComponentTests.Abstractions;
using Elsa.Workflows.ComponentTests.Fixtures;

namespace Elsa.Workflows.ComponentTests.Scenarios.Activities.Branching.Switch;

public class SwitchActivityTests(App app) : AppComponentTest(app)
{
    [Theory(DisplayName = "Switch activity should execute correct case")]
    [InlineData(nameof(SwitchMatchingCaseWorkflow), "Case B matched")]
    [InlineData(nameof(SwitchDefaultCaseWorkflow), "Default case")]
    public async Task Switch_ShouldExecuteCorrectCase(string workflowName, string expectedMessage)
    {
        // Arrange
        var definitionId = GetDefinitionId(workflowName);
        
        // Act
        var result = await RunWorkflowAsync(definitionId);
        
        // Assert
        AssertWorkflowFinished(result);
        var messages = GetWriteLineMessages(result);
        
        Assert.Single(messages);
        Assert.Equal(expectedMessage, messages[0]);
    }

    private static string GetDefinitionId(string workflowName) => workflowName switch
    {
        nameof(SwitchMatchingCaseWorkflow) => SwitchMatchingCaseWorkflow.DefinitionId,
        nameof(SwitchDefaultCaseWorkflow) => SwitchDefaultCaseWorkflow.DefinitionId,
        _ => throw new ArgumentException($"Unknown workflow: {workflowName}")
    };
}

