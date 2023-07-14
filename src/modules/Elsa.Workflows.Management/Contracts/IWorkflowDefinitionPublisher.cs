using Elsa.Common.Models;
using Elsa.Workflows.Core.Activities;
using Elsa.Workflows.Core.Contracts;
using Elsa.Workflows.Management.Entities;
using Elsa.Workflows.Management.Models;
using JetBrains.Annotations;

namespace Elsa.Workflows.Management.Contracts;

/// <summary>
/// Publishes workflow definitions.
/// </summary>
[PublicAPI]
public interface IWorkflowDefinitionPublisher
{
    /// <summary>
    /// Creates a new workflow definition.
    /// </summary>
    /// <param name="root">Optionally provide the root activity. If not specified, <see cref="Sequence" /> will be used/></param>
    /// <returns>The new workflow definition.</returns>
    WorkflowDefinition New(IActivity? root = default);

    /// <summary>
    /// Publishes a workflow definition.
    /// </summary>
    /// <param name="definitionId">The ID of the workflow definition to publish.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The published workflow definition.</returns>
    Task<PublishWorkflowDefinitionResult> PublishAsync(string definitionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Publishes a workflow definition.
    /// </summary>
    /// <param name="definition">The workflow definition to publish.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The published workflow definition.</returns>
    Task<PublishWorkflowDefinitionResult> PublishAsync(WorkflowDefinition definition, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retracts a workflow definition.
    /// </summary>
    /// <param name="definitionId">The ID of the workflow definition to retract.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The retracted workflow definition.</returns>
    Task<WorkflowDefinition?> RetractAsync(string definitionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retracts a workflow definition.
    /// </summary>
    /// <param name="definition">The workflow definition to retract.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The retracted workflow definition.</returns>
    Task<WorkflowDefinition> RetractAsync(WorkflowDefinition definition, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets or creates a draft for the specified workflow definition.
    /// </summary>
    /// <param name="definitionId">The ID of the workflow definition.</param>
    /// <param name="versionOptions">The version options.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The draft workflow definition.</returns>
    Task<WorkflowDefinition?> GetDraftAsync(string definitionId, VersionOptions versionOptions, CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves a draft for the specified workflow definition.
    /// </summary>
    /// <param name="definition">The workflow definition.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The saved workflow definition.</returns>
    Task<WorkflowDefinition> SaveDraftAsync(WorkflowDefinition definition, CancellationToken cancellationToken = default);
}