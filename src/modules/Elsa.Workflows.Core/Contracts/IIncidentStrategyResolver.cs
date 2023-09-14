namespace Elsa.Workflows.Core.Contracts;

/// <summary>
/// Resolves an incident strategy for a given activity execution context.
/// </summary>
public interface IIncidentStrategyResolver
{
    /// <summary>
    /// Resolves an incident strategy for a given activity execution context.
    /// </summary>
    ValueTask<IIncidentStrategy> ResolveStrategyAsync(ActivityExecutionContext context, CancellationToken cancellationToken = default);
}