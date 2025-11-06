using Elsa.Common.Models;
using Elsa.Testing.Shared.Services;
using Elsa.Workflows.ComponentTests.Abstractions;
using Elsa.Workflows.ComponentTests.Fixtures;
using Elsa.Workflows.ComponentTests.Scenarios.Activities.Timers.Timer.Workflows;
using Elsa.Workflows.Models;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Workflows.ComponentTests.Scenarios.Activities.Timers.Timer;

public class TimerTests : AppComponentTest
{
    private readonly AsyncWorkflowRunner _workflowRunner;

    public TimerTests(App app) : base(app)
    {
        _workflowRunner = Scope.ServiceProvider.GetRequiredService<AsyncWorkflowRunner>();
    }

    [Fact(DisplayName = "Timer activity blocks workflow execution and resumes after specified interval")]
    public async Task TimerActivity_BlocksAndResumes()
    {
        var result = await _workflowRunner.RunAndAwaitWorkflowCompletionAsync(
            WorkflowDefinitionHandle.ByDefinitionId(TimerWorkflow.DefinitionId, VersionOptions.Published));

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
