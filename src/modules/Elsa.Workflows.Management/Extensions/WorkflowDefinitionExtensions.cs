using Elsa.Workflows.Core.Models;
using Elsa.Workflows.Core.Services;
using Elsa.Workflows.Management.Entities;

namespace Elsa.Workflows.Management.Extensions;

/// <summary>
/// Adds extension methods for <see cref="WorkflowDefinition"/>.
/// </summary>
public static class WorkflowDefinitionExtensions
{
    /// <summary>
    /// Construct a new <see cref="Workflow"/> from the specified <see cref="WorkflowDefinition"/>.
    /// </summary>
    public static Workflow ToWorkflow(this WorkflowDefinition definition, IActivity root) =>
        new(
            new WorkflowIdentity(definition.DefinitionId, definition.Version, definition.Id),
            new WorkflowPublication(definition.IsLatest, definition.IsPublished),
            new WorkflowMetadata(definition.Name, definition.Description, definition.CreatedAt),
            definition.Options,
            root,
            definition.Variables,
            definition.CustomProperties);
}