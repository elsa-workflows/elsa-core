using Elsa.Api.Client.Shared.Models;
using Refit;

namespace Elsa.Api.Client.Resources.LogPersistenceStrategies;

/// <summary>
/// Represents a client for the variable types API.
/// </summary>
public interface ILogPersistenceStrategiesApi
{
    /// <summary>
    /// Lists log persistence strategies.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    [Get("/descriptors/log-persistence-strategies")]
    Task<ListResponse<LogPersistenceStrategyDescriptor>> ListAsync(CancellationToken cancellationToken = default);
}