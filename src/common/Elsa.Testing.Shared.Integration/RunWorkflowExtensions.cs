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
    /// <param name="services">The services.</param>
    extension(IServiceProvider services)
    {
        /// <summary>
        /// Runs a workflow until its end, automatically resuming any bookmark it encounters.
        /// </summary>
        /// <param name="workflowDefinitionId">The ID of the workflow definition.</param>
        /// <param name="input">An optional dictionary of input values.</param>
        /// <param name="correlationId">An optional correlation id of the workflow.</param>
        /// <param name="versionOptions">An optional set of options to specify the version of the workflow definition to retrieve.</param>
        /// <param name="runWorkflowOptions">Optional workflow execution options.</param>
        /// <returns>The workflow state.</returns>
        public async Task<WorkflowState> RunWorkflowUntilEndAsync(string workflowDefinitionId,
            IDictionary<string, object>? input = null,
            string? correlationId = null,
            VersionOptions? versionOptions = null,
            RunWorkflowOptions? runWorkflowOptions = null)
        {
            var workflowDefinitionService = services.GetRequiredService<IWorkflowDefinitionService>();
            var workflowGraph = await workflowDefinitionService.FindWorkflowGraphAsync(workflowDefinitionId, versionOptions ?? VersionOptions.Published);

            if (workflowGraph == null)
                throw new InvalidOperationException($"Workflow definition with ID '{workflowDefinitionId}' not found.");

            var workflowRuntime = services.GetRequiredService<IWorkflowRuntime>();
            var workflowClient = await workflowRuntime.CreateClientAsync();
            var response = await workflowClient.CreateAndRunInstanceAsync(new()
            {
                WorkflowDefinitionHandle = WorkflowDefinitionHandle.ByDefinitionVersionId(workflowGraph.Workflow.Identity.Id),
                Input = input,
                CorrelationId = correlationId,
                Properties = runWorkflowOptions?.Properties
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
                        BookmarkId = bookmark.Id,
                        Input = input,
                        Properties = runWorkflowOptions?.Properties
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
        public async Task<WorkflowState> RunWorkflowUntilEndAsync<TWorkflow>(IDictionary<string, object>? input = null) where TWorkflow : IWorkflow
        {
            var workflowDefinitionId = typeof(TWorkflow).Name;
            return await services.RunWorkflowUntilEndAsync(workflowDefinitionId, input);
        }
    }
}