using Elsa.Services;
using Elsa.Services.Models;

namespace Elsa.Events;

/// <summary>
/// Published when a workflow transitioned into the Faulted state but execution is still inside the worker loop.
/// </summary>
public class WorkflowFaulting : ActivityNotification
{
    public WorkflowFaulting(ActivityExecutionContext activityExecutionContext, IActivity activity) : base(activityExecutionContext, activity)
    {
    }
}