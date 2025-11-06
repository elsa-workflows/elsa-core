using Elsa.Common.Models;
using Elsa.Testing.Shared.Services;
using Elsa.Workflows.ComponentTests.Abstractions;
using Elsa.Workflows.ComponentTests.Fixtures;
using Elsa.Workflows.ComponentTests.Scenarios.Activities.Timers.Delay.Workflows;
using Elsa.Workflows.Models;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Workflows.ComponentTests.Scenarios.Activities.Timers.Delay;

public class DelayTests : AppComponentTest
{
    private readonly AsyncWorkflowRunner _workflowRunner;

    public DelayTests(App app) : base(app)
    {
        _workflowRunner = Scope.ServiceProvider.GetRequiredService<AsyncWorkflowRunner>();
    }

    [Fact(DisplayName = "Delay activity blocks workflow execution and resumes after specified duration")]
    public async Task DelayActivity_BlocksAndResumes()
    {
        var result = await _workflowRunner.RunAndAwaitWorkflowCompletionAsync(
            WorkflowDefinitionHandle.ByDefinitionId(DelayWorkflow.DefinitionId, VersionOptions.Published));

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
