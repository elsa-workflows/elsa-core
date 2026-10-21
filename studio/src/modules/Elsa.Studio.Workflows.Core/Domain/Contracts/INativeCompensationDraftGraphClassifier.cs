using System.Text.Json.Nodes;
using Elsa.Studio.Workflows.Domain.Models;

namespace Elsa.Studio.Workflows.Domain.Contracts;

/// <summary>
/// Classifies native compensation scopes only when reached through workflow activity edges.
/// </summary>
public interface INativeCompensationDraftGraphClassifier
{
    Task<NativeCompensationDraftGraphAnalysis> AnalyzeAsync(JsonObject? root, CancellationToken cancellationToken = default);
}
