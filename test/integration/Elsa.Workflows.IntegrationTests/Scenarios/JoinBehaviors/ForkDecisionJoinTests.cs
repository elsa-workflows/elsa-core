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

    [Fact(DisplayName = "The implicit join configured with Stream merge mode should execute.")]
    public async Task ImplicitJoinStreamShouldExecute()
    {
        await RunAndAssert("fork-decision-join-stream.json", ["A", "C"]);
    }

    [Fact(DisplayName = "The implicit join configured with Merge mode should not execute (waits for activated branches only).")]
    public async Task ImplicitJoinMergeShouldNotExecute()
    {
        await RunAndAssert("fork-decision-join-merge.json", ["A"]);
    }

    [Fact(DisplayName = "The implicit join configured with Converge mode should block (strictest - requires ALL inbound connections).")]
    public async Task ImplicitJoinConvergeShouldBlock()
    {
        await RunAndAssert("fork-decision-join-converge-strict.json", ["A"]);
    }

    [Fact(DisplayName = "The explicit join configured with WaitAll join mode should block.")]
    public async Task ExplicitJoinWaitAllShouldBlock()
    {
        await RunAndAssert("fork-decision-join-waitall.json", ["A", "C", "B", "D"]);
    }
    
    [Fact(DisplayName = "An implicit join from the True and False branches should execute because the join mode is Stream and by default, all active branches are joined.")]
    public async Task ImplicitJoinFromBranchesShouldExecute()
    {
        // Populate registries.
        await _services.PopulateRegistriesAsync();

        // Import workflow.
        var workflowDefinition = await _services.ImportWorkflowDefinitionAsync($"Scenarios/JoinBehaviors/Workflows/decision-merge-join-none.json");

        // Execute.
        var workflowState = await _services.RunWorkflowUntilEndAsync(workflowDefinition.DefinitionId);

        // Assert.
        Assert.Equal(WorkflowStatus.Finished, workflowState.Status);
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