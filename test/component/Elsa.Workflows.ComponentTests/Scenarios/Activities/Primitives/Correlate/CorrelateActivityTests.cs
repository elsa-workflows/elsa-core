using Elsa.Workflows.ComponentTests.Abstractions;
using Elsa.Workflows.ComponentTests.Fixtures;

namespace Elsa.Workflows.ComponentTests.Scenarios.Activities.Primitives.Correlate;

public class CorrelateActivityTests(App app) : AppComponentTest(app)
{
    [Theory(DisplayName = "Correlate activity should set workflow correlation ID")]
    [InlineData(nameof(CorrelateWorkflow), "my-correlation-id")]
    [InlineData(nameof(CorrelateUpdateWorkflow), "updated-correlation-id")]
    public async Task Correlate_ShouldSetCorrelationId(string workflowName, string expectedCorrelationId)
    {
        // Arrange
        var definitionId = GetDefinitionId(workflowName);
        
        // Act
        var result = await RunWorkflowAsync(definitionId);

        // Assert
        AssertWorkflowFinished(result);
        Assert.Equal(expectedCorrelationId, result.WorkflowExecutionContext.CorrelationId);
    }

    private static string GetDefinitionId(string workflowName) => workflowName switch
    {
        nameof(CorrelateWorkflow) => CorrelateWorkflow.DefinitionId,
        nameof(CorrelateUpdateWorkflow) => CorrelateUpdateWorkflow.DefinitionId,
        _ => throw new ArgumentException($"Unknown workflow: {workflowName}")
    };
}

