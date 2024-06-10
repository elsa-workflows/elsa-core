using Elsa.Common.Models;
using Elsa.Workflows;
using Elsa.Workflows.Contracts;
using Elsa.Workflows.Management.Contracts;
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
public static class ServiceProviderExtensions
{
    /// <summary>
    /// Updates the registries.
    /// </summary>
    /// <param name="services">The services.</param>
    public static Task PopulateRegistriesAsync(this IServiceProvider services)
    {
        var registriesPopulator = services.GetRequiredService<IRegistriesPopulator>();
        return registriesPopulator.PopulateAsync();
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
    /// <param name="versionOptions">An optional set of options to specify the version of the workflow definition to retrieve.</param>
    /// <returns>The workflow state.</returns>
    public static async Task<WorkflowState> RunWorkflowUntilEndAsync(this IServiceProvider services,
        string workflowDefinitionId,
        IDictionary<string, object>? input = default,
        VersionOptions? versionOptions = default)
    {
        var workflowDefinitionService = services.GetRequiredService<IWorkflowDefinitionService>();
        var workflowGraph = await workflowDefinitionService.FindWorkflowGraphAsync(workflowDefinitionId, versionOptions ?? VersionOptions.Published);
        var workflowRuntime = services.GetRequiredService<IWorkflowRuntime>();
        var workflowClient = await workflowRuntime.CreateClientAsync();
        var response = await workflowClient.CreateAndRunInstanceAsync(new CreateAndRunWorkflowInstanceRequest
        {
            WorkflowDefinitionHandle = WorkflowDefinitionHandle.ByDefinitionVersionId(workflowGraph!.Workflow.Identity.Id),
            Input = input
        });
        
        var bookmarkStore = services.GetRequiredService<IBookmarkStore>();

        // Continue resuming the workflow for as long as there are bookmarks to resume and the workflow is not Finished.
        while (response.Status != WorkflowStatus.Finished)
        {
            var bookmarks = (await bookmarkStore.FindManyAsync(new BookmarkFilter
            {
                WorkflowInstanceId = response.WorkflowInstanceId
            })).ToList();

            if (!bookmarks.Any())
                break;

            foreach (var bookmark in bookmarks)
            {
                var runRequest = new RunWorkflowInstanceRequest
                {
                    BookmarkId = bookmark.BookmarkId
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
    public static async Task<WorkflowState> RunWorkflowUntilEndAsync<TWorkflow>(this IServiceProvider services, IDictionary<string, object>? input = default) where TWorkflow : IWorkflow
    {
        var workflowDefinitionId = typeof(TWorkflow).Name;
        return await services.RunWorkflowUntilEndAsync(workflowDefinitionId, input);
    }

    /// <summary>
    /// Runs the specified activity.
    /// </summary>
    /// <param name="services">The service provider.</param>
    /// <param name="activity">The activity to run.</param>
    /// <param name="cancellationToken">An optional cancellation token.</param>
    /// <returns>The result of running the activity.</returns>
    public static async Task<RunWorkflowResult> RunActivityAsync(this IServiceProvider services, IActivity activity, CancellationToken cancellationToken = default)
    {
        await services.PopulateRegistriesAsync();
        var workflowRunner = services.GetRequiredService<IWorkflowRunner>();
        var result = await workflowRunner.RunAsync(activity, cancellationToken: cancellationToken);
        return result;
    }

    /// <summary>
    /// Runs the specified activity.
    /// </summary>
    /// <param name="services">The service provider.</param>
    /// <param name="activity">The activity to run.</param>
    /// <param name="options">An set of options.</param>
    /// <param name="cancellationToken">An optional cancellation token.</param>
    /// <returns>The result of running the activity.</returns>
    public static async Task<RunWorkflowResult> RunActivityAsync(this IServiceProvider services, IActivity activity, RunWorkflowOptions options, CancellationToken cancellationToken = default)
    {
        var workflowRunner = services.GetRequiredService<IWorkflowRunner>();
        var result = await workflowRunner.RunAsync(activity, options, cancellationToken);
        return result;
    }

    /// <summary>
    /// Retrieves a workflow definition by its ID.
    /// </summary>
    /// <param name="services">The service provider.</param>
    /// <param name="workflowDefinitionId">The definition ID of the workflow definition.</param>
    /// <param name="versionOptions">Options to specify the version of the workflow definition to retrieve.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>The retrieved workflow definition.</returns>
    public static async Task<WorkflowDefinition> GetWorkflowDefinitionAsync(this IServiceProvider services, string workflowDefinitionId, VersionOptions versionOptions, CancellationToken cancellationToken = default)
    {
        var workflowDefinitionService = services.GetRequiredService<IWorkflowDefinitionService>();
        var workflowDefinition = await workflowDefinitionService.FindWorkflowDefinitionAsync(workflowDefinitionId, versionOptions);
        return workflowDefinition!;
    }
}