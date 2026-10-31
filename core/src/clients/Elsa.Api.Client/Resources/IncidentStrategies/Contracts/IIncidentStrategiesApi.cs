using Elsa.Api.Client.Resources.IncidentStrategies.Models;
using Elsa.Api.Client.Shared.Models;
using Refit;

namespace Elsa.Api.Client.Resources.IncidentStrategies.Contracts;

/// <summary>
/// Represents a client for the variable types API.
/// </summary>
public interface IIncidentStrategiesApi
{
    /// <summary>
    /// Lists incident strategies.
    /// </summary>
    [Get("/descriptors/incident-strategies")]
    Task<ListResponse<IncidentStrategyDescriptor>> ListAsync(CancellationToken cancellationToken = default);
}