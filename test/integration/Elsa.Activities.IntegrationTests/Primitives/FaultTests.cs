using Elsa.Activities.IntegrationTests.Primitives.Workflows;
using Elsa.Testing.Shared;
using Elsa.Workflows;
using Elsa.Workflows.Activities;
using Elsa.Workflows.Exceptions;
using Elsa.Workflows.Models;
using Elsa.Workflows.State;
using Xunit.Abstractions;

namespace Elsa.Activities.IntegrationTests.Primitives;

/// <summary>
/// Integration tests for the <see cref="Fault"/> activity.
/// </summary>
public class FaultTests(ITestOutputHelper testOutputHelper)
{
    private readonly WorkflowTestFixture _fixture = new WorkflowTestFixture(testOutputHelper)
        .AddWorkflow<FaultWorkflow>()
        .AddWorkflow<FaultWithDefaultsWorkflow>()
        .AddWorkflow<FaultInSequenceWorkflow>()
        .AddWorkflow<FaultViaFactoryWorkflow>();

    [Fact(DisplayName = "Fault activity faults workflow execution")]
    public async Task Fault_Activity_Faults_Workflow_Execution()
    {
        // Act
        var workflowState = await _fixture.RunWorkflowAsync(FaultWorkflow.DefinitionId);

        // Assert
        AssertWorkflowFaulted(workflowState);
        var incident = GetSingleIncident(workflowState);
        Assert.Equal("Test fault message", incident.Message);
    }

    [Fact(DisplayName = "Fault activity with default values faults workflow")]
    public async Task Fault_With_Default_Values_Faults_Workflow()
    {
        // Act
        var workflowState = await _fixture.RunWorkflowAsync(FaultWithDefaultsWorkflow.DefinitionId);

        // Assert
        AssertWorkflowFaulted(workflowState);
        GetSingleIncident(workflowState);
    }

    [Fact(DisplayName = "Fault in sequence stops subsequent activities")]
    public async Task Fault_In_Sequence_Stops_Subsequent_Activities()
    {
        // Act
        var workflowState = await _fixture.RunWorkflowAsync(FaultInSequenceWorkflow.DefinitionId);

        // Assert
        AssertWorkflowFaulted(workflowState);

        var lines = _fixture.CapturingTextWriter.Lines.ToList();
        Assert.Contains("Before fault", lines);
        Assert.DoesNotContain("After fault - should not execute", lines);
    }

    [Fact(DisplayName = "Fault with factory method creates correct exception")]
    public async Task Fault_Created_With_Factory_Method()
    {
        // Act
        var workflowState = await _fixture.RunWorkflowAsync(FaultViaFactoryWorkflow.DefinitionId);

        // Assert
        AssertWorkflowFaulted(workflowState);
        var incident = GetSingleIncident(workflowState);
        Assert.Equal("Created via factory", incident.Message);
    }

    private static void AssertWorkflowFaulted(WorkflowState workflowState)
    {
        Assert.Equal(WorkflowSubStatus.Faulted, workflowState.SubStatus);
    }

    private static ActivityIncident GetSingleIncident(WorkflowState workflowState)
    {
        var incident = Assert.Single(workflowState.Incidents);
        Assert.NotNull(incident.Exception);
        Assert.Equal(typeof(FaultException), incident.Exception.Type);
        return incident;
    }
}