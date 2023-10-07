using Elsa.Common.Models;
using Elsa.Workflows.Core;
using Elsa.Workflows.Core.Contracts;
using Elsa.Workflows.Core.Models;
using Elsa.Workflows.Core.State;
using Elsa.Workflows.Management.Contracts;
using Elsa.Workflows.Management.Entities;
using Elsa.Workflows.Management.Models;
using Elsa.Workflows.Runtime.Contracts;
using Elsa.Workflows.Runtime.Options;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Testing.Shared;

/// <summary>
/// Provides extension methods for <see cref="IServiceProvider"/>.
/// </summary>
[PublicAPI]
public static class ServiceProviderExtensions
{
    /// <summary>
    /// Updates the registries.
    /// </summary>
    /// <param name="services">The services.</param>
    public static async Task PopulateRegistriesAsync(this IServiceProvider services)
    {
        var registriesPopulator = services.GetRequiredService<IRegistriesPopulator>();
        await registriesPopulator.PopulateAsync();
    }

    /// <summary>
    /// Imports a workflow definition from a file.
    /// </summary>
    /// <param name="services">The services.</param>
    /// <param name="fileName">The file name.</param>
    /// <returns>The workflow definition.</returns>
    public static async Task<WorkflowDefinition> ImportWorkflowDefinitionAsync(this IServiceProvider services, string fileName)
    {
        var json = await File.ReadAllTextAsync(fileName);
        var serializer = services.GetRequiredService<IActivitySerializer>();
        var model = serializer.Deserialize<WorkflowDefinitionModel>(json);

        var workflowDefinitionRequest = new SaveWorkflowDefinitionRequest
        {
            Model = model,
            Publish = true
        };

        var workflowDefinitionImporter = services.GetRequiredService<IWorkflowDefinitionImporter>();
        var result = await workflowDefinitionImporter.ImportAsync(workflowDefinitionRequest);
        return result.WorkflowDefinition;
    }

    /// <summary>
    /// Runs a workflow until its end, automatically resuming any bookmark it encounters.
    /// </summary>
    /// <param name="services">The services.</param>
    /// <param name="workflowDefinitionId">The ID of the workflow definition.</param>
    /// <param name="input">An optional dictionary of input values.</param>
    /// <returns>The workflow state.</returns>
    public static async Task<WorkflowState> RunWorkflowUntilEndAsync(this IServiceProvider services, string workflowDefinitionId, IDictionary<string, object>? input = default)
    {
        var startWorkflowOptions = new StartWorkflowRuntimeOptions(null, input, VersionOptions.Published);
        var workflowRuntime = services.GetRequiredService<IWorkflowRuntime>();
        var result = await workflowRuntime.StartWorkflowAsync(workflowDefinitionId, startWorkflowOptions);
        var bookmarks = new Stack<Bookmark>(result.Bookmarks);

        // Continue resuming the workflow for as long as there are bookmarks to resume and the workflow is not Finished.
        while (result.Status != WorkflowStatus.Finished && bookmarks.TryPop(out var bookmark))
        {
            var resumeOptions = new ResumeWorkflowRuntimeOptions(BookmarkId: bookmark.Id);
            var resumeResult = await workflowRuntime.ResumeWorkflowAsync(result.WorkflowInstanceId, resumeOptions);

            foreach (var newBookmark in resumeResult!.Bookmarks)
                bookmarks.Push(newBookmark);
        }

        // Return the workflow state.
        return (await workflowRuntime.ExportWorkflowStateAsync(result.WorkflowInstanceId))!;
    }

    /// <summary>
    /// Runs a workflow until its end, automatically resuming any bookmark it encounters.
    /// </summary>
    public static async Task<WorkflowState> RunWorkflowUntilEndAsync<TWorkflow>(this IServiceProvider services, IDictionary<string, object>? input = default) where TWorkflow : IWorkflow
    {
        var workflowDefinitionId = typeof(TWorkflow).Name;
        return await services.RunWorkflowUntilEndAsync(workflowDefinitionId, input);
    }
}