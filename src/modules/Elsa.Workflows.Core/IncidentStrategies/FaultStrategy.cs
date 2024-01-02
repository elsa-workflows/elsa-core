using System.ComponentModel;
using Elsa.Workflows.Contracts;
using JetBrains.Annotations;

// ReSharper disable once CheckNamespace
namespace Elsa.Workflows.IncidentStrategies;

/// <summary>
/// An incident strategy that puts the entire workflow in the <c>Faulted</c> state.
/// </summary>
[PublicAPI]
[Description("An incident strategy that puts the entire workflow in the Faulted state.")]
public class FaultStrategy : IIncidentStrategy
{
    /// <inheritdoc />
    public void HandleIncident(ActivityExecutionContext context)
    {
        var workflowExecutionContext = context.WorkflowExecutionContext;
        
        if(workflowExecutionContext.CanTransitionTo(WorkflowSubStatus.Faulted))
            workflowExecutionContext.TransitionTo(WorkflowSubStatus.Faulted);
    }
}