using Elsa.Common.Models;
using Elsa.Expressions.Contracts;
using Elsa.Workflows.Core.Contracts;
using Elsa.Workflows.Core.Models;
using Elsa.Workflows.Core.State;
using Elsa.Workflows.Management.Contracts;
using Elsa.Workflows.Management.Entities;
using Elsa.Workflows.Management.Models;
using Elsa.Workflows.Runtime.Contracts;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Testing.Shared;

/// <summary>
/// Provides extension methods for <see cref="IServiceProvider"/>.
/// </summary>
public static class ServiceProviderExtensions
{
    /// <summary>
    /// Updates the registries.
    /// </summary>
    /// <param name="services">The services.</param>
    public static async Task PopulateRegistriesAsync(this IServiceProvider services)
    {
        var activityRegistryPopulator = services.GetRequiredService<IActivityRegistryPopulator>();
        var expressionSyntaxRegistryPopulator = services.GetRequiredService<IExpressionSyntaxRegistryPopulator>();
        await activityRegistryPopulator.PopulateRegistryAsync();
        await expressionSyntaxRegistryPopulator.PopulateRegistryAsync();
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
        return await workflowDefinitionImporter.ImportAsync(workflowDefinitionRequest);
    }

    /// <summary>
    /// Runs a workflow until its end, automatically resuming any bookmark it encounters.
    /// </summary>
    /// <param name="services">The services.</param>
    /// <param name="workflowDefinitionId">The ID of the workflow definition.</param>
    /// <returns>The workflow state.</returns>
    public static async Task<WorkflowState> RunWorkflowUntilEndAsync(this IServiceProvider services, string workflowDefinitionId)
    {
        var startWorkflowOptions = new StartWorkflowRuntimeOptions(null, new Dictionary<string, object>(), VersionOptions.Published);
        var workflowRuntime = services.GetRequiredService<IWorkflowRuntime>();
        var result = await workflowRuntime.StartWorkflowAsync(workflowDefinitionId, startWorkflowOptions);
        var bookmarks = new Stack<Bookmark>(result.Bookmarks);

        // Continue resuming the workflow for as long as there are bookmarks to resume.
        while (bookmarks.TryPop(out var bookmark))
        {
            var resumeOptions = new ResumeWorkflowRuntimeOptions(BookmarkId: bookmark.Id);
            var resumeResult = await workflowRuntime.ResumeWorkflowAsync(result.WorkflowInstanceId, resumeOptions);

            foreach (var newBookmark in resumeResult.Bookmarks)
                bookmarks.Push(newBookmark);
        }

        // Return the workflow state.
        return (await workflowRuntime.ExportWorkflowStateAsync(result.WorkflowInstanceId))!;
    }
}