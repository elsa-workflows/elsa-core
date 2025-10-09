using Elsa.Testing.Shared;
using Xunit.Abstractions;

namespace Elsa.Workflows.IntegrationTests.Scenarios.ImplicitJoins;

public class DecisionBothOutcomesJoinedTests
{
    private readonly CapturingTextWriter _capturingTextWriter = new();
    private readonly IServiceProvider _services;

    public DecisionBothOutcomesJoinedTests(ITestOutputHelper testOutputHelper)
    {
        _services = new TestApplicationBuilder(testOutputHelper).WithCapturingTextWriter(_capturingTextWriter).Build();
    }

    [Fact(DisplayName = "Connecting both outcomes of a decision to an implicit join should result in the workflow completing.")]
    public async Task Test1()
    {
        // Populate registries.
        await _services.PopulateRegistriesAsync();

        // Import workflow.
        var workflowDefinition = await _services.ImportWorkflowDefinitionAsync("Scenarios/ImplicitJoins/Workflows/decision-implicit-join-on-both-outcomes.json");

        // Execute.
        var state = await _services.RunWorkflowUntilEndAsync(workflowDefinition.DefinitionId);

        // Assert.
        Assert.Equal(WorkflowStatus.Finished, state.Status);
    }
}