using System;
using System.Linq;
using System.Threading.Tasks;
using Elsa.Common.Models;
using Elsa.IntegrationTests.Scenarios.ImplicitJoins.Workflows;
using Elsa.Testing.Shared;
using Elsa.Workflows.Core;
using Elsa.Workflows.Core.Contracts;
using Elsa.Workflows.Runtime.Contracts;
using Elsa.Workflows.Runtime.Filters;
using Microsoft.Extensions.DependencyInjection;
using Open.Linq.AsyncExtensions;
using Xunit;
using Xunit.Abstractions;

namespace Elsa.IntegrationTests.Scenarios.ImplicitJoins;

public class JoinRunsOnceTests
{
    private readonly IWorkflowRunner _workflowRunner;
    private readonly CapturingTextWriter _capturingTextWriter = new();
    private readonly IServiceProvider _services;

    public JoinRunsOnceTests(ITestOutputHelper testOutputHelper)
    {
        _services = new TestApplicationBuilder(testOutputHelper).WithCapturingTextWriter(_capturingTextWriter).Build();
        _workflowRunner = _services.GetRequiredService<IWorkflowRunner>();
    }

    [Fact(DisplayName = "The Join activity executes only once, not twice")]
    public async Task Test1()
    {
        // Populate registries.
        await _services.PopulateRegistriesAsync();

        // Import workflow.
        var workflowDefinition = await _services.ImportWorkflowDefinitionAsync($"Scenarios/ImplicitJoins/Workflows/join.json");

        // Execute.
        var state = await _services.RunWorkflowUntilEndAsync(workflowDefinition.DefinitionId);
        
        // Assert.
        var journal = await _services.GetRequiredService<IWorkflowExecutionLogStore>().FindManyAsync(new WorkflowExecutionLogRecordFilter
        {
            WorkflowInstanceId = state.Id,
            ActivityId = "802725996be1b582",
            EventName = "Completed"
        }, PageArgs.All);

        Assert.Single(journal.Items);
    }
}