using Elsa.Common.Models;
using Elsa.Testing.Shared.Services;
using Elsa.Workflows.ComponentTests.Abstractions;
using Elsa.Workflows.ComponentTests.Fixtures;
using Elsa.Workflows.Models;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Workflows.ComponentTests.Scenarios.Activities.Timers;

public abstract class TimerActivityTestBase(App app) : AppComponentTest(app)
{
    protected async Task AssertActivityBlocksAndResumes(string workflowDefinitionId)
    {
        var workflowRunner = Scope.ServiceProvider.GetRequiredService<AsyncWorkflowRunner>();
        var result = await workflowRunner.RunAndAwaitWorkflowCompletionAsync(
            WorkflowDefinitionHandle.ByDefinitionId(workflowDefinitionId, VersionOptions.Published));

        Assert.Equal(WorkflowSubStatus.Finished, result.WorkflowExecutionContext.SubStatus);

        var writeLineRecords = result.ActivityExecutionRecords
            .Where(x => x.ActivityType == "Elsa.WriteLine")
            .OrderBy(x => x.CompletedAt)
            .ToList();

        Assert.Equal(2, writeLineRecords.Count);
        Assert.Equal("WriteLine1", writeLineRecords[0].ActivityId);
        Assert.Equal("WriteLine2", writeLineRecords[1].ActivityId);
    }
}
