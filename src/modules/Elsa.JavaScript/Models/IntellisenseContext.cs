using Elsa.Workflows.Core.Models;

namespace Elsa.JavaScript.Models;

/// <summary>
/// Provides context to intellisense providers.
/// </summary>
public record IntellisenseContext(Workflow Workflow, string? ActivityTypeName, string? PropertyName);