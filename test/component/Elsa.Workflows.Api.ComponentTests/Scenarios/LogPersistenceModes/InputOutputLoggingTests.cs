using Elsa.Api.Client.Resources.WorkflowDefinitions.Contracts;
using Elsa.Common.Entities;
using Elsa.Workflows.Activities;
using Elsa.Workflows.Api.Endpoints.WorkflowDefinitions.Execute;
using Elsa.Workflows.Helpers;
using Elsa.Workflows.Runtime.Contracts;
using Elsa.Workflows.Runtime.Filters;
using Elsa.Workflows.Runtime.OrderDefinitions;
using Microsoft.Extensions.DependencyInjection;
using Open.Linq.AsyncExtensions;
using Xunit.Abstractions;

namespace Elsa.Workflows.Api.ComponentTests.Scenarios.LogPersistenceModes;

public class InputOutputLoggingTests(ITestOutputHelper testOutputHelper, WorkflowServerWebAppFactoryFixture factoryFixture) : ComponentTest(testOutputHelper, factoryFixture)
{
    [Theory]
    [InlineData("input-output-logging-1", true, false, true, false)]
    [InlineData("input-output-logging-2", true, false, true, false)]
    public async Task HelloWorldWorkflow_ShouldRespondWithHelloWorld(string workflowDefinitionId, params bool[] shouldBeIncludedArray)
    {
        var client = FactoryFixture.CreateApiClient<IExecuteWorkflowApi>();
        using var response = await client.ExecuteAsync(workflowDefinitionId);
        var model = await response.ReadAsJsonAsync<Response>();
        var workflowInstanceId = model.WorkflowState.Id;
        var activityExecutionStore = Scope.ServiceProvider.GetRequiredService<IActivityExecutionStore>();
        var activityExecutionRecordFilter = new ActivityExecutionRecordFilter
        {
            WorkflowInstanceId = workflowInstanceId,
        };
        var writeLineActivityTypeName = ActivityTypeNameHelper.GenerateTypeName<WriteLine>();
        var orderBy = new ActivityExecutionRecordOrder<DateTimeOffset>(x => x.StartedAt, OrderDirection.Ascending);
        var activityExecutionRecords = await activityExecutionStore
            .FindManyAsync(activityExecutionRecordFilter, orderBy)
            .Where(x => x.ActivityType == writeLineActivityTypeName)
            .ToList();

        for (var i = 0; i < shouldBeIncludedArray.Length; i++)
        {
            var activityExecutionRecord = activityExecutionRecords[i];
            var shouldBeIncluded = shouldBeIncludedArray[i];
            var isIncluded = activityExecutionRecord.ActivityState?.ContainsKey(nameof(WriteLine.Text));
            Assert.Equal(shouldBeIncluded, isIncluded);
        }
    }
}