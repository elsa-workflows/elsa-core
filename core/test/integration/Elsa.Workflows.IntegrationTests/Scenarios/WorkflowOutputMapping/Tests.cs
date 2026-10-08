using Elsa.Testing.Shared;
using Xunit.Abstractions;

namespace Elsa.Workflows.IntegrationTests.Scenarios.WorkflowOutputMapping;

/// <summary>
/// Tests for mapping an activity's output directly to the workflow's output definition.
/// </summary>
public class Tests
{
    private readonly CapturingTextWriter _capturingTextWriter = new();
    private readonly IServiceProvider _services;

    public Tests(ITestOutputHelper testOutputHelper)
    {
        _services = new TestApplicationBuilder(testOutputHelper).WithCapturingTextWriter(_capturingTextWriter).Build();
    }

    [Fact(DisplayName = "Activity output mapped to workflow output definition is part of workflow instance output dictionary.")]
    public async Task Test1()
    {
        // Populate registries.
        await _services.PopulateRegistriesAsync();

        // Import child workflow.
        var workflowFileName = "Scenarios/WorkflowOutputMapping/Workflows/workflow-output.json";
        var workflowDefinition = await _services.ImportWorkflowDefinitionAsync(workflowFileName);

        // Execute.
        var workflowState = await _services.RunWorkflowUntilEndAsync(workflowDefinition.DefinitionId);

        // Assert expected output.
        var outputs = workflowState.Output;
        Assert.Contains("Output1", outputs.Keys);
        Assert.Equal("Foo", outputs["Output1"]);
    }
}