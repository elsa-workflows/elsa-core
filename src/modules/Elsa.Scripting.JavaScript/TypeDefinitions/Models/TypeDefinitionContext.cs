using Elsa.Workflows.Models;

namespace Elsa.Scripting.JavaScript.TypeDefinitions.Models;

/// <summary>
/// Provides context to intellisense providers.
/// </summary>
public record TypeDefinitionContext(WorkflowGraph WorkflowGraph, string? ActivityTypeName, string? PropertyName, CancellationToken CancellationToken);