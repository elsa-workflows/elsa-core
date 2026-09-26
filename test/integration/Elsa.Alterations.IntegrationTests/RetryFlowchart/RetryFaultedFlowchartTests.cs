using Elsa.Alterations.AlterationTypes;
using Elsa.Alterations.Core.Contracts;
using Elsa.Alterations.Extensions;
using Elsa.Common.Models;
using Elsa.Extensions;
using Elsa.Testing.Shared;
using Elsa.Workflows;
using Elsa.Workflows.Management;
using Elsa.Workflows.Models;
using Elsa.Workflows.Runtime;
using Elsa.Workflows.Runtime.Messages;
using Microsoft.Extensions.DependencyInjection;
using Xunit.Abstractions;

namespace Elsa.Alterations.IntegrationTests.RetryFlowchart;

/// <summary>
/// Reproduces the scenario where a Flowchart workflow using ContinueWithIncidentsStrategy faults on an
/// activity, is retried via a ScheduleActivity alteration referencing the activity ID, and is then expected
/// to run to completion. Previously the original faulted activity execution context was left in place,
/// which prevented the Flowchart from completing even after all remaining activities had run.
/// </summary>
[Collection(FlakyActivityTestCollection.Name)]
public class RetryFaultedFlowchartTests
{
    private readonly IServiceProvider _services;
    private readonly CapturingTextWriter _capturingTextWriter = new();
    private readonly IAlterationRunner _alterationRunner;
    private readonly IWorkflowInstanceStore _workflowInstanceStore;
    private readonly IWorkflowRuntime _workflowRuntime;

    public RetryFaultedFlowchartTests(ITestOutputHelper testOutputHelper)
    {
        _services = new TestApplicationBuilder(testOutputHelper)
            .WithCapturingTextWriter(_capturingTextWriter)
            .ConfigureElsa(elsa => elsa.UseAlterations())
            .AddWorkflow<FlakyFlowchartWorkflow>()
            .AddActivitiesFrom<FlakyActivity>()
            .Build();

        _alterationRunner = _services.GetRequiredService<IAlterationRunner>();
        _workflowInstanceStore = _services.GetRequiredService<IWorkflowInstanceStore>();
        _workflowRuntime = _services.GetRequiredService<IWorkflowRuntime>();
        FlakyActivity.Reset();
    }

    [Fact(DisplayName = "Faulted Flowchart workflow with ContinueWithIncidentsStrategy completes after retry alteration")]
    public async Task RetryingFaultedFlowchart_RunsToCompletion()
    {
        await _services.PopulateRegistriesAsync();

        var workflowClient = await _workflowRuntime.CreateClientAsync();
        await workflowClient.CreateInstanceAsync(new()
        {
            WorkflowDefinitionHandle = WorkflowDefinitionHandle.ByDefinitionId(nameof(FlakyFlowchartWorkflow), VersionOptions.Published)
        });
        var firstRun = await workflowClient.RunInstanceAsync(RunWorkflowInstanceRequest.Empty);

        // After the first run, the flaky activity should have faulted and the workflow should not have finished.
        Assert.NotEqual(WorkflowStatus.Finished, firstRun.Status);
        var stateAfterFault = await workflowClient.ExportStateAsync();
        Assert.Single(stateAfterFault.Incidents);
        Assert.Equal(FlakyFlowchartWorkflow.FlakyActivityId, stateAfterFault.Incidents.Single().ActivityId);
        Assert.Equal(["Start"], _capturingTextWriter.Lines);

        // Run a ScheduleActivity alteration for the faulted activity ID, mirroring what the Retry endpoint does.
        var alterations = new IAlteration[]
        {
            new ScheduleActivity { ActivityId = FlakyFlowchartWorkflow.FlakyActivityId }
        };
        var instanceIds = new[] { firstRun.WorkflowInstanceId };
        var alterationResults = await _alterationRunner.RunAsync(instanceIds, alterations);
        Assert.True(alterationResults.All(x => x.IsSuccessful));

        // Resume the workflow.
        var secondRun = await workflowClient.RunInstanceAsync(RunWorkflowInstanceRequest.Empty);

        // After the retry, the flowchart should run to the end and the workflow should be Finished.
        Assert.Equal(WorkflowStatus.Finished, secondRun.Status);
        Assert.Equal(["Start", "End"], _capturingTextWriter.Lines);

        var finalInstance = await _workflowInstanceStore.FindAsync(firstRun.WorkflowInstanceId);
        Assert.NotNull(finalInstance);
        Assert.Equal(WorkflowStatus.Finished, finalInstance!.Status);
    }
}
