using Elsa.Workflows.Management.Entities;

namespace Elsa.JavaScript.TypeDefinitions.Models;

/// <summary>
/// Provides context to intellisense providers.
/// </summary>
public record TypeDefinitionContext(WorkflowDefinition WorkflowDefinition, string? ActivityTypeName, string? PropertyName, CancellationToken CancellationToken);