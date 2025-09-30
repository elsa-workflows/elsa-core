using Elsa.Testing.Shared;
using Xunit.Abstractions;

namespace Elsa.Workflows.IntegrationTests.Scenarios.ImplicitJoins;

public class ForkDecisionJoinTests
{
    private readonly CapturingTextWriter _capturingTextWriter = new();
    private readonly IServiceProvider _services;

    public ForkDecisionJoinTests(ITestOutputHelper testOutputHelper)
    {
        _services = new TestApplicationBuilder(testOutputHelper).WithCapturingTextWriter(_capturingTextWriter).Build();
    }

    [Fact(DisplayName = "The implicit join configured with None merge mode should execute.")]
    public async Task ImplicitJoinNoneShouldExecute()
    {
        // Populate registries.
        await _services.PopulateRegistriesAsync();

        // Import workflow.
        var workflowDefinition = await _services.ImportWorkflowDefinitionAsync("Scenarios/ImplicitJoins/Workflows/fork-decision-join-none.json");

        // Execute.
        await _services.RunWorkflowUntilEndAsync(workflowDefinition.DefinitionId);
        
        // Assert.
        var lines = _capturingTextWriter.Lines.ToList();
        Assert.Equal(new[] { "A", "C" }, lines);
    }
    
    [Fact(DisplayName = "The implicit join configured with Converge merge mode should not execute.")]
    public async Task ImplicitJoinConvergeShouldNotExecute()
    {
        // Populate registries.
        await _services.PopulateRegistriesAsync();

        // Import workflow.
        var workflowDefinition = await _services.ImportWorkflowDefinitionAsync("Scenarios/ImplicitJoins/Workflows/fork-decision-join-converge.json");

        // Execute.
        await _services.RunWorkflowUntilEndAsync(workflowDefinition.DefinitionId);
        
        // Assert.
        var lines = _capturingTextWriter.Lines.ToList();
        Assert.Equal(new[] { "A" }, lines);
    }
}