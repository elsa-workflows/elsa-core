using Elsa.Workflows.Core.Contracts;

namespace Elsa.Workflows.Core.IncidentStrategies;

/// <summary>
/// An incident strategy that allows the workflow to continue running, but with incidents.
/// </summary>
public class ContinueWithIncidentsStrategy : IIncidentStrategy
{
    /// <inheritdoc />
    public void HandleIncident(ActivityExecutionContext context)
    {
        // Do nothing.
    }
}