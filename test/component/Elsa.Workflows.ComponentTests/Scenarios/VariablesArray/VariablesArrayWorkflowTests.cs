using Elsa.Common.Models;
using Elsa.Expressions.Helpers;
using Elsa.Extensions;
using Elsa.Workflows.ComponentTests.Abstractions;
using Elsa.Workflows.ComponentTests.Fixtures;
using Elsa.Workflows.ComponentTests.Scenarios.VariablesArray.Workflows;
using Elsa.Workflows.Management;
using Elsa.Workflows.Models;
using Elsa.Workflows.Runtime;
using Elsa.Workflows.Runtime.Entities;
using Elsa.Workflows.Runtime.Messages;
using Elsa.Workflows.State;
using Microsoft.Extensions.DependencyInjection;
using TUnit.Assertions.Enums;

namespace Elsa.Workflows.ComponentTests.Scenarios.VariablesArray;

public class VariablesArrayWorkflowTests(App app) : AppComponentTest(app)
{
    [Test]
    [DisplayName("Array variable is persisted across workflow runs")]
    public async Task VariableIsPersistedAcrossWorkflowRuns()
    {
        var workflowRuntime = Scope.ServiceProvider.GetRequiredService<IWorkflowRuntime>();
        var workflowClient = await workflowRuntime.CreateClientAsync();
        var workflowInstanceStore = Scope.ServiceProvider.GetRequiredService<IWorkflowInstanceStore>();
        var workflowDefinitionStore = Scope.ServiceProvider.GetRequiredService<IWorkflowDefinitionStore>();
        var bookmarkStore = Scope.ServiceProvider.GetRequiredService<IBookmarkStore>();
        var runAndCreateRequest = new CreateAndRunWorkflowInstanceRequest
        {
            WorkflowDefinitionHandle = WorkflowDefinitionHandle.ByDefinitionId(VariableArrayWorkflow.DefinitionId, VersionOptions.Latest),
        };
        var runResponse = await workflowClient.CreateAndRunInstanceAsync(runAndCreateRequest);
        var workflowInstanceId = runResponse.WorkflowInstanceId;
        var createdBookmarks = await bookmarkStore.FindManyAsync(new()
        {
            WorkflowInstanceId = workflowInstanceId
        });
        var bookmarks = new Stack<StoredBookmark>(createdBookmarks);
        var expectedElementLength = 3;

        var result = await workflowDefinitionStore.FindLastVersionAsync(new Management.Filters.WorkflowDefinitionFilter()
        {
            DefinitionId = VariableArrayWorkflow.DefinitionId
        }, default);

        await Assert.That(result?.Variables.FirstOrDefault(v => v.Id == "elementsVariable")?.Value as IEnumerable<string>)
            .IsEquivalentTo(["Element 1", "Element 2", "Element 3"], CollectionOrdering.Matching);

        while (bookmarks.Any())
        {
            var workflowInstance = await workflowInstanceStore.FindAsync(workflowInstanceId);
            var workflowState = workflowInstance!.WorkflowState;
            var rootWorkflowActivityExecutionContext = workflowState.ActivityExecutionContexts.Single(x => x.ParentContextId == null);
            var variables = GetVariablesDictionary(rootWorkflowActivityExecutionContext);
            var actualElements = variables["elementsVariable"].ConvertTo<string[]>();
            await Assert.That(actualElements?.Length).IsEqualTo(--expectedElementLength);

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
        await Assert.That(expectedElementLength).IsEqualTo(0);
    }

    private VariablesDictionary GetVariablesDictionary(ActivityExecutionContextState context)
    {
        return context.Properties.GetOrAdd(WorkflowInstanceStorageDriver.VariablesDictionaryStateKey, () => new VariablesDictionary());
    }
}
