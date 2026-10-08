using Elsa.Testing.Shared;
using Xunit.Abstractions;

namespace Elsa.Workflows.IntegrationTests.Scenarios.ParentChildCompletion;

/// <summary>
/// Tests for mapping an activity's output directly to the workflow's output definition.
/// </summary>
public class Tests
{
    private readonly IServiceProvider _services;

    public Tests(ITestOutputHelper testOutputHelper)
    {
        _services = new TestApplicationBuilder(testOutputHelper).Build();
    }

    [Fact(DisplayName = "Parent workflow invoking child workflow as activity completes successfully.")]
    public async Task Test1()
    {
        // Populate registries.
        await _services.PopulateRegistriesAsync();

        // Import child workflow.
        var childWorkflowFilename = "Scenarios/ParentChildCompletion/Workflows/child.json";
        await _services.ImportWorkflowDefinitionAsync(childWorkflowFilename);

        // Import parent workflow.
        var parentWorkflowFilename = "Scenarios/ParentChildCompletion/Workflows/parent.json";
        var parentWorkflowDefinition = await _services.ImportWorkflowDefinitionAsync(parentWorkflowFilename);

        // Execute.
        var workflowState = await _services.RunWorkflowUntilEndAsync(parentWorkflowDefinition.DefinitionId);

        // Assert expected status
        Assert.Equal(WorkflowStatus.Finished, workflowState.Status);
    }
}