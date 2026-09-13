using Elsa.Testing.Shared;

namespace Elsa.Workflows.IntegrationTests.Scenarios.WorkflowOutputMapping;

/// <summary>
/// Tests for mapping an activity's output directly to the workflow's output definition.
/// </summary>
public class Tests : IAsyncDisposable
{
    private readonly CapturingTextWriter _capturingTextWriter = new();
    private readonly IServiceProvider _services;

    public Tests()
    {
        _services = new TestApplicationBuilder(TestContext.Current!.Output.StandardOutput).WithCapturingTextWriter(_capturingTextWriter).Build();
    }

    [Test]
    [DisplayName("Activity output mapped to workflow output definition is part of workflow instance output dictionary.")]
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
        await Assert.That(outputs.Keys).Contains("Output1");
        await Assert.That(outputs["Output1"]).IsEqualTo("Foo");
    }

    public ValueTask DisposeAsync() => TestResourceDisposal.DisposeAsync(_services);
}
