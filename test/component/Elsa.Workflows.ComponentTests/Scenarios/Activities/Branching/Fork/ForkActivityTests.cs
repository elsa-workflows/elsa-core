using Elsa.Workflows.ComponentTests.Abstractions;
using Elsa.Workflows.ComponentTests.Fixtures;

namespace Elsa.Workflows.ComponentTests.Scenarios.Activities.Branching.Fork;

public class ForkActivityTests(App app) : AppComponentTest(app)
{
    [Theory(DisplayName = "Fork activity should execute all branches")]
    [InlineData(nameof(ForkBasicWorkflow), new[] { "Branch A", "Branch B", "Branch C" })]
    [InlineData(nameof(ForkWaitAllWorkflow), new[] { "Branch A - Step 1", "Branch B - Step 1" })]
    public async Task Fork_ShouldExecuteAllBranches(string workflowName, string[] expectedMessageFragments)
    {
        // Arrange
        var definitionId = GetDefinitionId(workflowName);
        
        // Act
        var result = await RunWorkflowAsync(definitionId);
        
        // Assert
        AssertWorkflowFinished(result);
        var messages = GetWriteLineMessages(result);
        
        foreach (var expected in expectedMessageFragments)
        {
            Assert.Contains(expected, messages);
        }
    }

    private static string GetDefinitionId(string workflowName) => workflowName switch
    {
        nameof(ForkBasicWorkflow) => ForkBasicWorkflow.DefinitionId,
        nameof(ForkWaitAllWorkflow) => ForkWaitAllWorkflow.DefinitionId,
        _ => throw new ArgumentException($"Unknown workflow: {workflowName}")
    };
}

