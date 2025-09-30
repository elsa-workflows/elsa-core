using Elsa.Common.Models;
using Elsa.Testing.Shared;
using Elsa.Workflows.Runtime;
using Microsoft.Extensions.DependencyInjection;
using Xunit.Abstractions;

namespace Elsa.Workflows.IntegrationTests.Scenarios.JoinBehaviors;

public class ParallelJoinCompletesTests
{
    private readonly CapturingTextWriter _capturingTextWriter = new();
    private readonly IServiceProvider _services;

    public ParallelJoinCompletesTests(ITestOutputHelper testOutputHelper)
    {
        _services = new TestApplicationBuilder(testOutputHelper).WithCapturingTextWriter(_capturingTextWriter).Build();
    }

    [Fact(DisplayName = "The ParallelForEach activity completes when its Body contains a Join activity")]
    public async Task Test1()
    {
        // Populate registries.
        await _services.PopulateRegistriesAsync();

        // Import workflow.
        var workflowDefinition = await _services.ImportWorkflowDefinitionAsync("Scenarios/ImplicitJoins/Workflows/parallel-join.json");

        // Execute.
        var state = await _services.RunWorkflowUntilEndAsync(workflowDefinition.DefinitionId);
        
        // Assert.
        var journal = await _services.GetRequiredService<IWorkflowExecutionLogStore>().FindManyAsync(new()
        {
            WorkflowInstanceId = state.Id,
            ActivityId = "70fc1183cd5800f2",
            EventName = "Completed"
        }, PageArgs.All);

        Assert.Single(journal.Items);
    }
}