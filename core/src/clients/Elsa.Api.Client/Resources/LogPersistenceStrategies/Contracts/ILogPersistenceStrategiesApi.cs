using Elsa.Api.Client.Shared.Models;
using JetBrains.Annotations;
using Refit;

namespace Elsa.Api.Client.Resources.LogPersistenceStrategies;

/// <summary>
/// Represents a client API for managing log persistence strategies.
/// </summary>
[UsedImplicitly]
public interface ILogPersistenceStrategiesApi
{
    /// <summary>
    /// Lists log persistence strategies.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    [Get("/descriptors/log-persistence-strategies")]
    Task<ListResponse<LogPersistenceStrategyDescriptor>> ListAsync(CancellationToken cancellationToken = default);
}