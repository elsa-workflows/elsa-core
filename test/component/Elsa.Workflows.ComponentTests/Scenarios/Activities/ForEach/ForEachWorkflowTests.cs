using Elsa.Common.Models;
using Elsa.Workflows.ComponentTests.Abstractions;
using Elsa.Workflows.ComponentTests.Fixtures;
using Elsa.Workflows.ComponentTests.Scenarios.Activities.ForEach.Workflows;
using Elsa.Workflows.ComponentTests.Services;
using Elsa.Workflows.Models;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Workflows.ComponentTests.Scenarios.Activities.ForEach;

public class ForEachWorkflowTests : AppComponentTest
{
    private readonly AsyncWorkflowRunner _workflowRunner;

    public ForEachWorkflowTests(App app) : base(app)
    {
        _workflowRunner = Scope.ServiceProvider.GetRequiredService<AsyncWorkflowRunner>();
    }

    [Fact(DisplayName = "ForEach activity executes child activity for each collection item and supports blocking activities")]
    public async Task ForEachActivity_ExecutesChildActivity_ForEachCollectionItem_AndSupportsBlocking()
    {
        var result = await _workflowRunner.RunAndAwaitWorkflowCompletionAsync(WorkflowDefinitionHandle.ByDefinitionId(ForEachWorkflow.DefinitionId, VersionOptions.Published));
        var writeLineExecutionRecords = result.ActivityExecutionRecords.Where(x => x.ActivityId == "WriteLine1").ToList();
        
        // Assert that the workflow executed the expected number of activities.
        Assert.Equal(3, writeLineExecutionRecords.Count);
    }
}