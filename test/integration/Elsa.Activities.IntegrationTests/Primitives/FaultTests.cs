using Elsa.Activities.IntegrationTests.Primitives.Workflows;
using Elsa.Testing.Shared;
using Elsa.Workflows;
using Elsa.Workflows.Activities;
using Elsa.Workflows.Exceptions;
using Elsa.Workflows.Models;
using Elsa.Workflows.State;

namespace Elsa.Activities.IntegrationTests.Primitives;

/// <summary>
/// Integration tests for the <see cref="Fault"/> activity.
/// </summary>
public class FaultTests : IAsyncDisposable
{
    private readonly WorkflowTestFixture _fixture = new WorkflowTestFixture(TestContext.Current!.Output.StandardOutput)
        .AddWorkflow<FaultWorkflow>()
        .AddWorkflow<FaultWithDefaultsWorkflow>()
        .AddWorkflow<FaultInSequenceWorkflow>()
        .AddWorkflow<FaultViaFactoryWorkflow>();

    public ValueTask DisposeAsync() => _fixture.DisposeAsync();

    [Test]
    [DisplayName("Fault activity faults workflow execution")]
    public async Task Fault_Activity_Faults_Workflow_Execution()
    {
        // Act
        var workflowState = await _fixture.RunWorkflowAsync(FaultWorkflow.DefinitionId);

        // Assert
        await AssertWorkflowFaulted(workflowState);
        var incident = await GetSingleIncident(workflowState);
        await Assert.That(incident.Message).IsEqualTo("Test fault message");

    }

    [Test]
    [DisplayName("Fault activity with default values faults workflow")]
    public async Task Fault_With_Default_Values_Faults_Workflow()
    {
        // Act
        var workflowState = await _fixture.RunWorkflowAsync(FaultWithDefaultsWorkflow.DefinitionId);

        // Assert
        await AssertWorkflowFaulted(workflowState);
        await GetSingleIncident(workflowState);
    }

    [Test]
    [DisplayName("Fault in sequence stops subsequent activities")]
    public async Task Fault_In_Sequence_Stops_Subsequent_Activities()
    {
        // Act
        var workflowState = await _fixture.RunWorkflowAsync(FaultInSequenceWorkflow.DefinitionId);

        // Assert
        await AssertWorkflowFaulted(workflowState);

        var lines = _fixture.CapturingTextWriter.Lines.ToList();
        await Assert.That(lines).Contains("Before fault");

        await Assert.That(lines).DoesNotContain("After fault - should not execute");

    }

    [Test]
    [DisplayName("Fault with factory method creates correct exception")]
    public async Task Fault_Created_With_Factory_Method()
    {
        // Act
        var workflowState = await _fixture.RunWorkflowAsync(FaultViaFactoryWorkflow.DefinitionId);

        // Assert
        await AssertWorkflowFaulted(workflowState);
        var incident = await GetSingleIncident(workflowState);
        await Assert.That(incident.Message).IsEqualTo("Created via factory");

    }

    private static async Task AssertWorkflowFaulted(WorkflowState workflowState)
    {
        await Assert.That(workflowState.SubStatus).IsEqualTo(WorkflowSubStatus.Faulted);

    }

    private static async Task<ActivityIncident> GetSingleIncident(WorkflowState workflowState)
    {
        var incident = (await Assert.That(workflowState.Incidents).HasSingleItem())!;

        var exception = (await Assert.That(incident.Exception).IsNotNull())!;
        await Assert.That(exception.Type).IsEqualTo(typeof(FaultException));

        return incident;
    }
}
