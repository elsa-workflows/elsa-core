using JetBrains.Annotations;

namespace Elsa.Api.Client.Models;

/// <summary>
/// A summary of a workflow definition.
/// </summary>
[PublicAPI]
public record WorkflowDefinitionSummary(string Id, string DefinitionId, string? Name, string? Description, int? Version, bool IsLatest, bool IsPublished, string MaterializerName, DateTimeOffset CreatedAt);