using Elsa.Testing.Shared;
using Xunit.Abstractions;

namespace Elsa.Workflows.IntegrationTests.Scenarios.JoinBehaviors;

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
        await RunAndAssert("fork-decision-join-none.json", ["A", "C"]);
    }
    
    [Fact(DisplayName = "The implicit join configured with Converge merge mode should not execute.")]
    public async Task ImplicitJoinConvergeShouldNotExecute()
    {
        await RunAndAssert("fork-decision-join-converge.json", ["A"]);
    }
    
    [Fact(DisplayName = "The explicit join configured with WaitAll join mode should block.")]
    public async Task ExplicitJoinWaitAllShouldBlock()
    {
        await RunAndAssert("fork-decision-join-waitall.json", ["A", "C", "B", "D"]);
    }
    
    private async Task RunAndAssert(string workflowFileName, string[] expectedLines)
    {
        // Populate registries.
        await _services.PopulateRegistriesAsync();

        // Import workflow.
        var workflowDefinition = await _services.ImportWorkflowDefinitionAsync($"Scenarios/JoinBehaviors/Workflows/{workflowFileName}");

        // Execute.
        await _services.RunWorkflowUntilEndAsync(workflowDefinition.DefinitionId);
        
        // Assert.
        var lines = _capturingTextWriter.Lines.ToList();
        Assert.Equal(expectedLines, lines);
    }
}