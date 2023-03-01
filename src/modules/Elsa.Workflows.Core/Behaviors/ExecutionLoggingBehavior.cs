using System.Text.Json;
using Elsa.Extensions;
using Elsa.Workflows.Core.Models;
using Elsa.Workflows.Core.Services;
using Elsa.Workflows.Core.Signals;

namespace Elsa.Workflows.Core.Behaviors;

/// <summary>
/// Records Start and Completed entries of the owning activity. 
/// </summary>
public class ExecutionLoggingBehavior : Behavior
{
    public ExecutionLoggingBehavior(IActivity owner) : base(owner)
    {
        OnSignalReceived<ActivityCompleted>(OnActivityCompleted);
        OnSignalReceived<ActivityFaulted>(OnActivityFaulted);
    }

    protected override void Execute(ActivityExecutionContext context)
    {
        context.AddExecutionLogEntry("Started");
    }
    
    private void OnActivityCompleted(ActivityCompleted signal, SignalContext context)
    {
        if (context.IsSelf)
        {
            context.SenderActivityExecutionContext.AddExecutionLogEntry("Completed",
                payload: JsonSerializer.Serialize(context.SenderActivityExecutionContext.Input));
        }
    }
    
    private void OnActivityFaulted(ActivityFaulted signal, SignalContext context)
    {
        if (!context.IsSelf) return;
        
        var exception = context.SenderActivityExecutionContext.WorkflowExecutionContext.Fault?.Exception;
        context.SenderActivityExecutionContext.AddExecutionLogEntry("Faulted",
            payload: new
            {
                Exception = new
                {
                    exception?.Message,
                    exception?.Source,
                    exception?.Data,
                    Type = exception?.GetType()
                }
            });
    }
}