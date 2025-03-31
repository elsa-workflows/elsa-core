using Elsa.Common.Models;
using Elsa.Workflows;
using Elsa.Workflows.Management;
using Elsa.Workflows.Management.Entities;
using Elsa.Workflows.Management.Models;
using Elsa.Workflows.Models;
using Elsa.Workflows.Options;
using Elsa.Workflows.Runtime;
using Elsa.Workflows.Runtime.Filters;
using Elsa.Workflows.Runtime.Messages;
using Elsa.Workflows.State;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Testing.Shared;

/// <summary>
/// Provides extension methods for <see cref="IServiceProvider"/>.
/// </summary>
[PublicAPI]
public static class RunWorkflowExtensions
{
    /// <summary>
    /// Runs a workflow until its end, automatically resuming any bookmark it encounters.
    /// </summary>
    /// <param name="services">The services.</param>
    /// <param name="workflowDefinitionId">The ID of the workflow definition.</param>
    /// <param name="input">An optional dictionary of input values.</param>
    /// <param name="versionOptions">An optional set of options to specify the version of the workflow definition to retrieve.</param>
    /// <returns>The workflow state.</returns>
    public static async Task<WorkflowState> RunWorkflowUntilEndAsync(this IServiceProvider services,
        string workflowDefinitionId,
        IDictionary<string, object>? input = null,
        VersionOptions? versionOptions = null)
    {
        var workflowDefinitionService = services.GetRequiredService<IWorkflowDefinitionService>();
        var workflowGraph = await workflowDefinitionService.FindWorkflowGraphAsync(workflowDefinitionId, versionOptions ?? VersionOptions.Published);
        var workflowRuntime = services.GetRequiredService<IWorkflowRuntime>();
        var workflowClient = await workflowRuntime.CreateClientAsync();
        var response = await workflowClient.CreateAndRunInstanceAsync(new()
        {
            WorkflowDefinitionHandle = WorkflowDefinitionHandle.ByDefinitionVersionId(workflowGraph!.Workflow.Identity.Id),
            Input = input
        });
        
        var bookmarkStore = services.GetRequiredService<IBookmarkStore>();

        // Continue resuming the workflow for as long as there are bookmarks to resume and the workflow is not Finished.
        while (response.Status != WorkflowStatus.Finished)
        {
            var bookmarks = (await bookmarkStore.FindManyAsync(new()
            {
                WorkflowInstanceId = response.WorkflowInstanceId
            })).ToList();

            if (!bookmarks.Any())
                break;

            foreach (var bookmark in bookmarks)
            {
                var runRequest = new RunWorkflowInstanceRequest
                {
                    BookmarkId = bookmark.Id
                };
                response = await workflowClient.RunInstanceAsync(runRequest);
            }
        }

        // Return the workflow state.
        return await workflowClient.ExportStateAsync();
    }

    /// <summary>
    /// Runs a workflow until its end, automatically resuming any bookmark it encounters.
    /// </summary>
    public static async Task<WorkflowState> RunWorkflowUntilEndAsync<TWorkflow>(this IServiceProvider services, IDictionary<string, object>? input = null) where TWorkflow : IWorkflow
    {
        var workflowDefinitionId = typeof(TWorkflow).Name;
        return await services.RunWorkflowUntilEndAsync(workflowDefinitionId, input);
    }
}