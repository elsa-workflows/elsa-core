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
        var workflowDefinition = await workflowDefinitionService.FindWorkflowDefinitionAsync(workflowDefinitionId, versionOptions, cancellationToken);
        return workflowDefinition!;
    }
}