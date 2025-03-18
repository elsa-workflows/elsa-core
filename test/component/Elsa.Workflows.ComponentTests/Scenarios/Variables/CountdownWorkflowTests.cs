using Elsa.Common.Models;
using Elsa.Expressions.Helpers;
using Elsa.Extensions;
using Elsa.Workflows.ComponentTests.Abstractions;
using Elsa.Workflows.ComponentTests.Fixtures;
using Elsa.Workflows.ComponentTests.Scenarios.Variables.Workflows;
using Elsa.Workflows.Management;
using Elsa.Workflows.Models;
using Elsa.Workflows.Runtime;
using Elsa.Workflows.Runtime.Entities;
using Elsa.Workflows.Runtime.Filters;
using Elsa.Workflows.Runtime.Messages;
using Elsa.Workflows.State;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Workflows.ComponentTests.Scenarios.Variables;

public class CountdownWorkflowTests(App app) : AppComponentTest(app)
{
    [Fact(DisplayName = "Variable is persisted across workflow runs")]
    public async Task VariableIsPersistedAcrossWorkflowRuns()
    {
        var workflowRuntime = Scope.ServiceProvider.GetRequiredService<IWorkflowRuntime>();
        var workflowClient = await workflowRuntime.CreateClientAsync();
        var workflowInstanceStore = Scope.ServiceProvider.GetRequiredService<IWorkflowInstanceStore>();
        var bookmarkStore = Scope.ServiceProvider.GetRequiredService<IBookmarkStore>();
        var runAndCreateRequest = new CreateAndRunWorkflowInstanceRequest
        {
            WorkflowDefinitionHandle = WorkflowDefinitionHandle.ByDefinitionId(CountdownWorkflow.DefinitionId, VersionOptions.Latest),
        };
        var runResponse = await workflowClient.CreateAndRunInstanceAsync(runAndCreateRequest);
        var workflowInstanceId = runResponse.WorkflowInstanceId;
        var createdBookmarks = await bookmarkStore.FindManyAsync(new()
        {
            WorkflowInstanceId = workflowInstanceId
        });
        var bookmarks = new Stack<StoredBookmark>(createdBookmarks);
        var expectedCounter = 3;

        while (bookmarks.Any())
        {
            var workflowInstance = await workflowInstanceStore.FindAsync(workflowInstanceId);
            var workflowState = workflowInstance!.WorkflowState;
            var rootWorkflowActivityExecutionContext = workflowState.ActivityExecutionContexts.Single(x => x.ParentContextId == null);
            var variables = GetVariablesDictionary(rootWorkflowActivityExecutionContext);
            var actualCounter = variables["counterVariable"].ConvertTo<int>();
            Assert.Equal(--expectedCounter, actualCounter);

            var bookmark = bookmarks.Pop();
            var runRequest = new RunWorkflowInstanceRequest
            {
                BookmarkId = bookmark.Id,
            };
            
            await workflowClient.RunInstanceAsync(runRequest);

            createdBookmarks = await bookmarkStore.FindManyAsync(new()
            {
                WorkflowInstanceId = workflowInstanceId
            });

            foreach (var newBookmark in createdBookmarks)
                bookmarks.Push(newBookmark);
        }
    }

    private VariablesDictionary GetVariablesDictionary(ActivityExecutionContextState context)
    {
        return context.Properties.GetOrAdd(WorkflowInstanceStorageDriver.VariablesDictionaryStateKey, () => new VariablesDictionary());
    }
}