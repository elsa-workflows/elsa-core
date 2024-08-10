using Elsa.Workflows.Models;

namespace Elsa.JavaScript.TypeDefinitions.Models;

/// Provides context to intellisense providers.
public record TypeDefinitionContext(WorkflowGraph WorkflowGraph, string? ActivityTypeName, string? PropertyName, CancellationToken CancellationToken);