using Elsa.Workflows.ComponentTests.Abstractions;
using Elsa.Workflows.ComponentTests.Fixtures;

namespace Elsa.Workflows.ComponentTests.Scenarios.Activities.Branching.Parallel;

public class ParallelActivityTests(App app) : AppComponentTest(app)
{
    [Theory(DisplayName = "Parallel activity should execute all branches")]
    [InlineData(nameof(ParallelBasicWorkflow), 4, new[] { "Branch 1", "Branch 2", "Branch 3", "All branches completed" })]
    [InlineData(nameof(ParallelWithSequencesWorkflow), 7, null)]  // 6 branch steps + final message
    public async Task Parallel_ShouldExecuteAllBranches(string workflowName, int expectedWriteLineCount, string[]? expectedMessages)
    {
        // Arrange
        var definitionId = GetDefinitionId(workflowName);
        
        // Act
        var result = await RunWorkflowAsync(definitionId);
        
        // Assert
        AssertWorkflowFinished(result);
        var writeLineRecords = GetWriteLineRecords(result);
        Assert.Equal(expectedWriteLineCount, writeLineRecords.Count);
        
        if (expectedMessages != null)
        {
            var messages = GetWriteLineMessages(result);
            foreach (var expected in expectedMessages)
            {
                Assert.Contains(expected, messages);
            }
        }
    }

    private static string GetDefinitionId(string workflowName) => workflowName switch
    {
        nameof(ParallelBasicWorkflow) => ParallelBasicWorkflow.DefinitionId,
        nameof(ParallelWithSequencesWorkflow) => ParallelWithSequencesWorkflow.DefinitionId,
        _ => throw new ArgumentException($"Unknown workflow: {workflowName}")
    };
}

