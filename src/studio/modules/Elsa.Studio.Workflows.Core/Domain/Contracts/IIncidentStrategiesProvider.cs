using Elsa.Api.Client.Resources.IncidentStrategies.Models;

namespace Elsa.Studio.Workflows.Domain.Contracts;

/// <summary>
/// Provides incident strategies.
/// </summary>
public interface IIncidentStrategiesProvider
{
    /// <summary>
    /// Gets incident strategies.
    /// </summary>
    ValueTask<IEnumerable<IncidentStrategyDescriptor>> GetIncidentStrategiesAsync(CancellationToken cancellationToken = default);
}