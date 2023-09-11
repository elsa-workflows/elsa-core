namespace Elsa.Workflows.Core.Contracts;

/// <summary>
/// A strategy for handling workflow incidents
/// </summary>
public interface IIncidentStrategy
{
    /// <summary>
    /// Handles an incident.
    /// </summary>
    /// <param name="context">The activity execution context where the incident occurred.</param>
    void HandleIncident(ActivityExecutionContext context);
}