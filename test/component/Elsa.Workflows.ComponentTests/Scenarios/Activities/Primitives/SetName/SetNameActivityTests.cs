using Elsa.Workflows.ComponentTests.Abstractions;
using Elsa.Workflows.ComponentTests.Fixtures;

namespace Elsa.Workflows.ComponentTests.Scenarios.Activities.Primitives.SetName;

public class SetNameActivityTests(App app) : AppComponentTest(app)
{
    [Theory(DisplayName = "SetName activity should set workflow instance name")]
    [InlineData(nameof(SetNameWorkflow), "My Custom Workflow Name")]
    [InlineData(nameof(SetDynamicNameWorkflow), "Order-12345")]
    public async Task SetName_ShouldSetWorkflowInstanceName(string workflowName, string expectedName)
    {
        // Arrange
        var definitionId = GetDefinitionId(workflowName);
        
        // Act
        var result = await RunWorkflowAsync(definitionId);

        // Assert
        AssertWorkflowFinished(result);
        Assert.Equal(expectedName, result.WorkflowExecutionContext.Name);
    }

    private static string GetDefinitionId(string workflowName) => workflowName switch
    {
        nameof(SetNameWorkflow) => SetNameWorkflow.DefinitionId,
        nameof(SetDynamicNameWorkflow) => SetDynamicNameWorkflow.DefinitionId,
        _ => throw new ArgumentException($"Unknown workflow: {workflowName}")
    };
}


