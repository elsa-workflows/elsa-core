using Elsa.Common.Models;
using Elsa.Testing.Shared.Services;
using Elsa.Workflows.ComponentTests.Abstractions;
using Elsa.Workflows.ComponentTests.Fixtures;
using Elsa.Workflows.ComponentTests.Scenarios.Activities.Timers.StartAt.Workflows;
using Elsa.Workflows.Models;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Workflows.ComponentTests.Scenarios.Activities.Timers.StartAt;

public class StartAtTests : AppComponentTest
{
    private readonly AsyncWorkflowRunner _workflowRunner;

    public StartAtTests(App app) : base(app)
    {
        _workflowRunner = Scope.ServiceProvider.GetRequiredService<AsyncWorkflowRunner>();
    }

    [Fact(DisplayName = "StartAt activity blocks workflow execution and resumes at specified time")]
    public async Task StartAtActivity_BlocksAndResumes()
    {
        var result = await _workflowRunner.RunAndAwaitWorkflowCompletionAsync(
            WorkflowDefinitionHandle.ByDefinitionId(StartAtWorkflow.DefinitionId, VersionOptions.Published));

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
