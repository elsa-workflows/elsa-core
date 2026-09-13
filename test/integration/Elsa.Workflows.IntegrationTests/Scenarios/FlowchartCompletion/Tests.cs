using Elsa.Testing.Shared;

namespace Elsa.Workflows.IntegrationTests.Scenarios.FlowchartCompletion;

/// <summary>
/// Tests for the flowchart completion feature using various workflow setups.
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
    [DisplayName("Workflows should complete successfully: $workflowFileName")]
    [Arguments("workflow1.json")]
    [Arguments("workflow2.json")]
    [Arguments("workflow3.json")]
    [Arguments("workflow4.json")]
    [Arguments("workflow5.json")]
    [Arguments("workflow6.json")]
    public async Task Test1(string workflowFileName)
    {
        // Populate registries.
        await _services.PopulateRegistriesAsync();
        
        // Import workflow.
        var fileName = $"Scenarios/FlowchartCompletion/Workflows/{workflowFileName}";
        var workflowDefinition = await _services.ImportWorkflowDefinitionAsync(fileName);

        // Execute.
        var workflowState = await _services.RunWorkflowUntilEndAsync(workflowDefinition.DefinitionId);
        
        // Assert that the workflow has completed.
        await Assert.That(workflowState.Status).IsEqualTo(WorkflowStatus.Finished);
    }

    public ValueTask DisposeAsync() => TestResourceDisposal.DisposeAsync(_services);
}
