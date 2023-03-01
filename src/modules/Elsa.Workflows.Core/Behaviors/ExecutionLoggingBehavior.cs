using Elsa.Extensions;
using Elsa.Workflows.Core.Models;
using Elsa.Workflows.Core.Services;
using Elsa.Workflows.Core.Signals;
using Newtonsoft.Json.Linq;
using JsonSerializer = Newtonsoft.Json.JsonSerializer;

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
        var payload = new JObject();

        foreach (var entry in context.SenderActivityExecutionContext.JournalData)
            payload[entry.Key] = entry.Value != null ? JToken.FromObject(entry.Value, JsonSerializer.CreateDefault()) : JValue.CreateNull();
        
        if (context.IsSelf)
            context.SenderActivityExecutionContext.AddExecutionLogEntry("Completed", payload: payload);
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