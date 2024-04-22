using Elsa.Api.Client.Resources.WorkflowDefinitions.Contracts;
using Elsa.Common.Entities;
using Elsa.Workflows.Activities;
using Elsa.Workflows.Api.Endpoints.WorkflowDefinitions.Execute;
using Elsa.Workflows.Helpers;
using Elsa.Workflows.Runtime.Contracts;
using Elsa.Workflows.Runtime.Entities;
using Elsa.Workflows.Runtime.Filters;
using Elsa.Workflows.Runtime.OrderDefinitions;
using Elsa.Workflows.State;
using Microsoft.Extensions.DependencyInjection;
using Open.Linq.AsyncExtensions;

namespace Elsa.Workflows.ComponentTests.Scenarios.LogPersistenceModes;

public class InputOutputLoggingTests(WorkflowServerWebAppFactoryFixture factoryFixture) : ComponentTest(factoryFixture)
{
    [Theory]
    [InlineData("input-output-logging-1", true, false, true, false)]
    [InlineData("input-output-logging-2", true, false, true, false)]
    public async Task Workflow_ShouldHonorSettings_WhenExecuting(string workflowDefinitionId, params bool[] shouldBeIncludedArray)
    {
        var workflowState = await ExecuteWorkflowAsync(workflowDefinitionId);
        var workflowInstanceId = workflowState.Id;
        var writeLineActivityTypeName = ActivityTypeNameHelper.GenerateTypeName<WriteLine>();
        var activityExecutionRecords = await GetActivityExecutionRecordsAsync(workflowInstanceId, x => x.ActivityType == writeLineActivityTypeName).ToList();

        for (var i = 0; i < shouldBeIncludedArray.Length; i++)
        {
            var activityExecutionRecord = activityExecutionRecords[i];
            var shouldBeIncluded = shouldBeIncludedArray[i];
            var isIncluded = activityExecutionRecord.ActivityState!.ContainsKey(nameof(WriteLine.Text));
            Assert.Equal(shouldBeIncluded, isIncluded);
        }
    }

    [Fact]
    public async Task WorkflowAsActivity_ShouldHonorSettings_WhenExecuting()
    {
        await ExecuteWorkflowAsync("input-output-logging-3");
        var activityExecutionRecord = await GetRecordByActivityNameAsync("InputOutputLogging3-Child1");
        var output1IsIncluded = activityExecutionRecord.Outputs!.ContainsKey("Output1");
        var output2IsIncluded = activityExecutionRecord.Outputs!.ContainsKey("Output2");

        Assert.True(output1IsIncluded);
        Assert.True(output2IsIncluded);
    }
    
    [Fact]
    public async Task WorkflowAsActivityInternal_ShouldHonorSettings_WhenExecuting()
    {
        await ExecuteWorkflowAsync("input-output-logging-3");
        var setOutput1Record = await GetRecordByActivityNameAsync("SetOutput1");
        var setOutput2Record = await GetRecordByActivityNameAsync("SetOutput2");
        var output1IsIncluded = setOutput1Record.ActivityState!.ContainsKey("OutputName");
        var output2IsIncluded = setOutput2Record.ActivityState!.ContainsKey("OutputName");

        Assert.False(output1IsIncluded);
        Assert.True(output2IsIncluded);
    }

    private async Task<WorkflowState> ExecuteWorkflowAsync(string workflowDefinitionId)
    {
        var client = FactoryFixture.CreateApiClient<IExecuteWorkflowApi>();
        using var response = await client.ExecuteAsync(workflowDefinitionId);
        var model = await response.ReadAsJsonAsync<Response>();
        return model.WorkflowState;
    }

    private Task<IEnumerable<ActivityExecutionRecord>> GetActivityExecutionRecordsAsync(string workflowInstanceId, Func<ActivityExecutionRecord, bool> predicate)
    {
        var activityExecutionRecordFilter = new ActivityExecutionRecordFilter
        {
            WorkflowInstanceId = workflowInstanceId,
        };
        return GetActivityExecutionRecordsAsync(activityExecutionRecordFilter, predicate);
    }

    private Task<ActivityExecutionRecord> GetRecordByActivityNameAsync(string name)
    {
        var filter = new ActivityExecutionRecordFilter
        {
            Name = name
        };
        return GetActivityExecutionRecordAsync(filter);
    }

    private async Task<IEnumerable<ActivityExecutionRecord>> GetActivityExecutionRecordsAsync(ActivityExecutionRecordFilter filter, Func<ActivityExecutionRecord, bool>? predicate = null)
    {
        var activityExecutionStore = Scope.ServiceProvider.GetRequiredService<IActivityExecutionStore>();
        var orderBy = new ActivityExecutionRecordOrder<DateTimeOffset>(x => x.StartedAt, OrderDirection.Ascending);
        var activityExecutionRecords = await activityExecutionStore.FindManyAsync(filter, orderBy);
        return predicate == null ? activityExecutionRecords : activityExecutionRecords.Where(predicate);
    }

    private async Task<ActivityExecutionRecord> GetActivityExecutionRecordAsync(ActivityExecutionRecordFilter filter)
    {
        var activityExecutionStore = Scope.ServiceProvider.GetRequiredService<IActivityExecutionStore>();
        var result = await activityExecutionStore.FindAsync(filter);
        
        if(result == null)
            throw new InvalidOperationException($"Activity execution record not found.");
        
        return result;
    }
}