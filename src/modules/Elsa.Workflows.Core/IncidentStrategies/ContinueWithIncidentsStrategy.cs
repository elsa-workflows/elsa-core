using System.ComponentModel;
using Elsa.Workflows.Core;
using Elsa.Workflows.Core.Contracts;
using JetBrains.Annotations;

// ReSharper disable once CheckNamespace
namespace Elsa.Workflows.IncidentStrategies;

/// <summary>
/// An incident strategy that allows the workflow to continue running, but with incidents.
/// </summary>
[PublicAPI]
[Description("An incident strategy that allows the workflow to continue running, but with incidents.")]
public class ContinueWithIncidentsStrategy : IIncidentStrategy
{
    /// <inheritdoc />
    public void HandleIncident(ActivityExecutionContext context)
    {
        // Do nothing.
    }
}