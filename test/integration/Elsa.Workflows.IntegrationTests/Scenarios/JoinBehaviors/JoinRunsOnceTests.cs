using Elsa.Common.Models;
using Elsa.Testing.Shared;
using Elsa.Workflows.Activities.Flowchart.Extensions;
using Elsa.Workflows.Options;
using Elsa.Workflows.Runtime;
using Microsoft.Extensions.DependencyInjection;
using Xunit.Abstractions;

namespace Elsa.Workflows.IntegrationTests.Scenarios.JoinBehaviors;

public class JoinRunsOnceTests(ITestOutputHelper testOutputHelper)
{
    private readonly WorkflowTestFixture _fixture = new(testOutputHelper);

    [Fact(DisplayName = "The Join activity executes only once, not twice")]
    public async Task Test1()
    {
        // Import workflow.
        var workflowDefinition = await _fixture.ImportWorkflowDefinitionAsync($"Scenarios/JoinBehaviors/Workflows/join.json");

        // Execute with token-based mode.
        var options = new RunWorkflowOptions().WithTokenBasedFlowchart();
        var state = await _fixture.RunWorkflowAsync(workflowDefinition.DefinitionId, options: options);

        // Assert.
        var journal = await _fixture.Services.GetRequiredService<IWorkflowExecutionLogStore>().FindManyAsync(new()
        {
            WorkflowInstanceId = state.Id,
            ActivityId = "802725996be1b582",
            EventName = "Completed"
        }, PageArgs.All);

        Assert.Single(journal.Items);
    }
}