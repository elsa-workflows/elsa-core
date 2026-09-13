using Elsa.Api.Client.Resources.WorkflowDefinitions.Contracts;
using Elsa.Api.Client.Resources.WorkflowDefinitions.Responses;
using Elsa.Testing.Shared.Extensions;
using Elsa.Workflows.Activities;
using Elsa.Workflows.ComponentTests.Abstractions;
using Elsa.Workflows.ComponentTests.Fixtures;
using Elsa.Workflows.Helpers;
using Elsa.Workflows.Runtime;
using Elsa.Workflows.Runtime.Entities;
using Elsa.Workflows.Runtime.Filters;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Workflows.ComponentTests.Scenarios.LogPersistenceModes;

public class InternalStateLoggingTests(App app) : AppComponentTest(app)
{
    [Fact]
    public async Task ActivityInternalState_ShouldBeIndependentOfDefaultMode()
    {
        var records = await ExecuteAndGetWriteLinesAsync("internal-state-logging-activity");

        AssertInternalState(GetRecord(records, "WriteLine1"), textIncluded: false, internalStateIncluded: true);
        AssertInternalState(GetRecord(records, "WriteLine2"), textIncluded: true, internalStateIncluded: false);
    }

    [Fact]
    public async Task WorkflowInternalState_ShouldApplyWhenActivityInternalStateIsMissing()
    {
        var records = await ExecuteAndGetWriteLinesAsync("internal-state-logging-workflow");

        AssertInternalState(GetRecord(records, "WriteLine1"), textIncluded: false, internalStateIncluded: true);
        AssertInternalState(GetRecord(records, "WriteLine2"), textIncluded: false, internalStateIncluded: false);
    }

    private static ActivityExecutionRecord GetRecord(IReadOnlyList<ActivityExecutionRecord> records, string activityName)
    {
        return records.Single(x => x.ActivityName == activityName);
    }

    private static void AssertInternalState(ActivityExecutionRecord record, bool textIncluded, bool internalStateIncluded)
    {
        Assert.Equal(textIncluded, record.ActivityState?.ContainsKey(nameof(WriteLine.Text)) == true);

        if (internalStateIncluded)
        {
            Assert.NotNull(record.Properties);
            return;
        }

        Assert.Null(record.Properties);
        Assert.Null(record.Payload);
    }

    private async Task<IReadOnlyList<ActivityExecutionRecord>> ExecuteAndGetWriteLinesAsync(string workflowDefinitionId)
    {
        var client = WorkflowServer.CreateApiClient<IExecuteWorkflowApi>();
        using var response = await client.ExecuteAsync(workflowDefinitionId);
        var model = await response.ReadAsJsonAsync<ExecuteWorkflowDefinitionResponse>(WorkflowServer.Services);
        var writeLineActivityTypeName = ActivityTypeNameHelper.GenerateTypeName<WriteLine>();
        var store = Scope.ServiceProvider.GetRequiredService<IActivityExecutionStore>();
        var records = await store.FindManyAsync(new ActivityExecutionRecordFilter
        {
            WorkflowInstanceId = model.WorkflowState.Id
        });

        return records.Where(x => x.ActivityType == writeLineActivityTypeName).ToList();
    }
}
