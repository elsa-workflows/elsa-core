using Elsa.Alterations.Core.Results;

namespace Elsa.Alterations.Endpoints.Alterations.Run;

/// <summary>
/// The response from the <see cref="Run"/> endpoint.
/// </summary>
public record Response(ICollection<RunAlterationsResult> Results);