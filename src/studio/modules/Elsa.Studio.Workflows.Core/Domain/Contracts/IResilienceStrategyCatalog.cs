using System.Text.Json.Nodes;

namespace Elsa.Studio.Workflows.Domain.Contracts;

/// <summary>
/// Provides resilience strategies.
/// </summary>
public interface IResilienceStrategyCatalog
{
    /// <summary>
    /// Gets resilience strategies for the specified category.
    /// </summary>
    ValueTask<IEnumerable<JsonObject>> ListAsync(string category, CancellationToken cancellationToken = default);
}