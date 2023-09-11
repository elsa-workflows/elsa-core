using Elsa.Workflows.Core.Contracts;
using Elsa.Workflows.Core.Models;
using JetBrains.Annotations;

namespace Elsa.Workflows.Core.IncidentStrategies;

/// <summary>
/// An incident strategy that puts the entire workflow in the <c>Faulted</c> state.
/// </summary>
[PublicAPI]
public class FaultStrategy : IIncidentStrategy
{
    /// <inheritdoc />
    public void HandleIncident(ActivityExecutionContext context)
    {
        var workflowExecutionContext = context.WorkflowExecutionContext;
        var exception = context.Exception;
        
        workflowExecutionContext.Fault = new WorkflowFault(exception, exception?.Message ?? "Error", context.Id);
        
        if(workflowExecutionContext.CanTransitionTo(WorkflowSubStatus.Faulted))
            workflowExecutionContext.TransitionTo(WorkflowSubStatus.Faulted);
    }
}