using Elsa.Workflows.Core.Activities;

namespace Elsa.JavaScript.TypeDefinitions.Models;

/// <summary>
/// Provides context to intellisense providers.
/// </summary>
public record TypeDefinitionContext(Workflow Workflow, string? ActivityTypeName, string? PropertyName, CancellationToken CancellationToken);