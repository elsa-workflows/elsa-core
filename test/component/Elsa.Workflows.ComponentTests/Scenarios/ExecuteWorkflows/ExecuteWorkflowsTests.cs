using Elsa.Common.Entities;
using Elsa.Common.Models;
using Elsa.Workflows.Activities;
using Elsa.Workflows.ComponentTests.Abstractions;
using Elsa.Workflows.ComponentTests.Fixtures;
using Elsa.Workflows.ComponentTests.Scenarios.ExecuteWorkflows.Workflows;
using Elsa.Workflows.Models;
using Elsa.Workflows.Runtime;
using Elsa.Workflows.Runtime.Filters;
using Elsa.Workflows.Runtime.Messages;
using Elsa.Workflows.Runtime.OrderDefinitions;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Workflows.ComponentTests.Scenarios.ExecuteWorkflows;

public class ExecuteWorkflowsTests : AppComponentTest
{
    private readonly IWorkflowRuntime _workflowRuntime;

    public ExecuteWorkflowsTests(App app) : base(app)
    {
        _workflowRuntime = Scope.ServiceProvider.GetRequiredService<IWorkflowRuntime>();
    }

    [Fact]
    public async Task ExecuteWorkflow_ShouldExecuteWorkflow()
    {
        var workflowClient = await _workflowRuntime.CreateClientAsync();
        await workflowClient.CreateInstanceAsync(new CreateWorkflowInstanceRequest
        {
            WorkflowDefinitionHandle = WorkflowDefinitionHandle.ByDefinitionId(MainWorkflow.DefinitionId, VersionOptions.Published),
            CorrelationId = "test-correlation-id",
            Input = new Dictionary<string, object>
            {
                ["Value"] = 2163
            }
        });
        
        var response = await workflowClient.RunInstanceAsync(RunWorkflowInstanceRequest.Empty);

        // child activity
        var filter = new ActivityExecutionRecordFilter { };
        var activityExecutionStore = Scope.ServiceProvider.GetRequiredService<IActivityExecutionStore>();
        var orderBy = new ActivityExecutionRecordOrder<DateTimeOffset>(x => x.StartedAt, OrderDirection.Ascending);
        var activityExecutionRecords = await activityExecutionStore.FindManyAsync(filter, orderBy);

        var textEntries = activityExecutionRecords
            .Where(r => r.ActivityState != null && r.WorkflowInstanceId != response.WorkflowInstanceId)
            .SelectMany(r => r.ActivityState.Where(x => x.Key == nameof(WriteLine.Text)))
            .Select(r => r.Value)
            .ToList();
        Assert.Single(textEntries);
        Assert.Equal("Running subroutine on value 2163 ..., correlation id is 'test-correlation-id'.", textEntries[0]);
        
        // parent activity
        textEntries = activityExecutionRecords
            .Where(r => r.ActivityState != null && r.WorkflowInstanceId == response.WorkflowInstanceId)
            .SelectMany(r => r.ActivityState.Where(x => x.Key == nameof(WriteLine.Text)))
            .Select(r => r.Value)
            .ToList();
        Assert.Single(textEntries);
        var childWorkflowInstanceId = activityExecutionRecords.Select(r => r.WorkflowInstanceId).Where(r => r != response.WorkflowInstanceId).FirstOrDefault();
        Assert.Equal("Subroutine output: {\"WorkflowDefinitionVersionId\":null,\"WorkflowInstanceId\":\"" + childWorkflowInstanceId +
            "\",\"CorrelationId\":\"test-correlation-id\",\"Status\":1,\"SubStatus\":3,\"Output\":{\"Output\":4326}}", textEntries[0]);
    }
}