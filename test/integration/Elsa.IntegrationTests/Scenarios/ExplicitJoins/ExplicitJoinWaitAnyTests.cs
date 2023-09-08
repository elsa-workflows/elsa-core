using System;
using System.Linq;
using System.Threading.Tasks;
using Elsa.Testing.Shared;
using Elsa.Workflows.Core;
using Xunit;
using Xunit.Abstractions;

namespace Elsa.IntegrationTests.Scenarios.ExplicitJoins;

public class ExplicitJoinWaitAnyTests
{
    private readonly CapturingTextWriter _capturingTextWriter = new();
    private readonly IServiceProvider _services;

    public ExplicitJoinWaitAnyTests(ITestOutputHelper testOutputHelper)
    {
        _services = new TestApplicationBuilder(testOutputHelper).WithCapturingTextWriter(_capturingTextWriter).Build();
    }

    [Fact(DisplayName = "Workflows with explicit joins using WaitAny wait for any of the specified activities to complete and complete the workflow.")]
    public async Task Test1()
    {
        // Populate registries.
        await _services.PopulateRegistriesAsync();

        // Import workflow.
        var fileName = "Scenarios/ExplicitJoins/Workflows/flow-join-any.json";
        var workflowDefinition = await _services.ImportWorkflowDefinitionAsync(fileName);

        // Execute.
        var workflowState = await _services.RunWorkflowUntilEndAsync(workflowDefinition.DefinitionId);

        // Assert expected output.
        var lines = _capturingTextWriter.Lines.ToList();
        Assert.Equal(new[] { "Start", "End" }, lines);

        // Assert expected workflow status.
        Assert.Equal(WorkflowStatus.Finished, workflowState.Status);
    }
}