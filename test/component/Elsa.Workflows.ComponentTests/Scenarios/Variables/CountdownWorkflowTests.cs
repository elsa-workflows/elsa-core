using Elsa.Expressions.Helpers;
using Elsa.Extensions;
using Elsa.Workflows.ComponentTests.Scenarios.Variables.Workflows;
using Elsa.Workflows.Management.Contracts;
using Elsa.Workflows.Models;
using Elsa.Workflows.Runtime.Contracts;
using Elsa.Workflows.Runtime.Parameters;
using Elsa.Workflows.Services;
using Elsa.Workflows.State;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Workflows.ComponentTests.Scenarios.Variables;

public class CountdownWorkflowTests : AppComponentTest
{
    /// <inheritdoc />
    public CountdownWorkflowTests(App app) : base(app)
    {
    }

    [Fact(DisplayName = "Variable is persisted across workflow runs")]
    public async Task VariableIsPersistedAcrossWorkflowRuns()
    {
        var workflowRuntime = Scope.ServiceProvider.GetRequiredService<IWorkflowRuntime>();
        var workflowInstanceStore = Scope.ServiceProvider.GetRequiredService<IWorkflowInstanceStore>();
        var startParams = new StartWorkflowRuntimeParams();
        var result = await workflowRuntime.StartWorkflowAsync(CountdownWorkflow.DefinitionId, startParams);
        var workflowInstanceId = result.WorkflowInstanceId;
        var bookmarks = new Stack<Bookmark>(result.Bookmarks);
        var expectedCounter = 3;

        while (bookmarks.Any())
        {
            var workflowInstance = await workflowInstanceStore.FindAsync(workflowInstanceId);
            var workflowState = workflowInstance!.WorkflowState;
            var rootWorkflowActivityExecutionContext = workflowState.ActivityExecutionContexts.Single(x => x.ParentContextId == null);
            var variables = GetVariablesDictionary(rootWorkflowActivityExecutionContext);
            var actualCounter = variables["Workflow1:variable-1"].ConvertTo<int>();
            Assert.Equal(--expectedCounter, actualCounter);

            var bookmark = bookmarks.Pop();
            var resumeWorkflowRuntimeOptions = new ResumeWorkflowRuntimeParams
            {
                BookmarkId = bookmark?.Id,
            };

            result = await workflowRuntime.ResumeWorkflowAsync(workflowInstanceId, resumeWorkflowRuntimeOptions);

            if (result == null)
                break;

            foreach (var newBookmark in result.Bookmarks) bookmarks.Push(newBookmark);
        }
    }

    private IDictionary<string, object> GetVariablesDictionary(ActivityExecutionContextState context) =>
        context.Properties.GetOrAdd(WorkflowStorageDriver.VariablesDictionaryStateKey, () => new Dictionary<string, object>());
}