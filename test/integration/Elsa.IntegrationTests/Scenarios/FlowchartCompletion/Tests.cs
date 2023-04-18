using System;
using System.Threading.Tasks;
using Elsa.Testing.Shared;
using Elsa.Workflows.Core.Models;
using Xunit;
using Xunit.Abstractions;

namespace Elsa.IntegrationTests.Scenarios.FlowchartCompletion;

/// <summary>
/// Tests for the flowchart completion feature using various workflow setups.
/// </summary>
public class Tests
{
    private readonly CapturingTextWriter _capturingTextWriter = new();
    private readonly IServiceProvider _services;

    public Tests(ITestOutputHelper testOutputHelper)
    {
        _services = new TestApplicationBuilder(testOutputHelper).WithCapturingTextWriter(_capturingTextWriter).Build();
    }

    [Theory(DisplayName = "Workflows should complete successfully.")]
    [InlineData("workflow1.json")]
    [InlineData("workflow2.json")]
    [InlineData("workflow3.json")]
    [InlineData("workflow4.json")]
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
        Assert.Equal(WorkflowStatus.Finished, workflowState.Status);
    }
}
