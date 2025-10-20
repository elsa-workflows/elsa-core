using Elsa.Testing.Shared;
using Xunit.Abstractions;

namespace Elsa.Workflows.IntegrationTests.Scenarios.ExplicitJoins;

public class ExplicitJoinWaitAllInboundTests
{
    private readonly CapturingTextWriter _capturingTextWriter = new();
    private readonly IServiceProvider _services;

    public ExplicitJoinWaitAllInboundTests(ITestOutputHelper testOutputHelper)
    {
        _services = new TestApplicationBuilder(testOutputHelper)
            .WithCapturingTextWriter(_capturingTextWriter)
            .Build();
    }

    [Fact(DisplayName = "Workflows with WaitAllInbound join mode block when not all inbound connections are followed.")]
    public async Task Test1()
    {
        // Populate registries.
        await _services.PopulateRegistriesAsync();

        // Import workflow.
        var fileName = "Scenarios/ExplicitJoins/Workflows/join-wait-all-inbound.json";
        var workflowDefinition = await _services.ImportWorkflowDefinitionAsync(fileName);

        // Execute.
        // This workflow has a FlowDecision with condition=false, so only the "False" branch executes.
        // The FlowJoin is configured with WaitAllInbound mode, which requires ALL inbound connections to be followed.
        // Since only one of two inbound connections is followed, the join should block and the workflow should NOT complete.
        var workflowState = await _services.RunWorkflowUntilEndAsync(workflowDefinition.DefinitionId);

        // Assert that the workflow is blocked (not completed).
        Assert.Equal(WorkflowStatus.Running, workflowState.Status);
        Assert.Equal(WorkflowSubStatus.Suspended, workflowState.SubStatus);
    }
}
