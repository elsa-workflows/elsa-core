using System.Runtime.CompilerServices;
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
    }

    protected override void Execute(ActivityExecutionContext context)
    {
        context.AddExecutionLogEntry("Started");
    }
    
    private void OnActivityCompleted(ActivityCompleted signal, SignalContext context)
    {
        if (context.IsSelf)
            context.SenderActivityExecutionContext.AddExecutionLogEntry("Completed");
    }
}