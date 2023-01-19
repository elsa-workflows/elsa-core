using Elsa.Workflows.Core.Models;

namespace Elsa.JavaScript.Models;

/// <summary>
/// Provides context to intellisense providers.
/// </summary>
public record TypeDefinitionContext(ICollection<Variable> Variables, string? ActivityTypeName, string? PropertyName, CancellationToken CancellationToken);