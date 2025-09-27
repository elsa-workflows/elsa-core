using Elsa.Alterations.Core.Results;

namespace Elsa.Alterations.Endpoints.Workflows.Retry;

/// <summary>
/// The response from the <see cref="Retry"/> endpoint.
/// </summary>
public record Response(ICollection<RunAlterationsResult> Results);