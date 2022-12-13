using System.ComponentModel;
using System.Text.Json.Serialization;
using Elsa.Workflows.Core.Attributes;
using Elsa.Workflows.Core.Models;

namespace Elsa.Workflows.Core.Activities;

[Activity("Elsa", "Control Flow", "Set the CorrelationId of the workflow to a given value.")]
public class Correlate : Activity
{
    [JsonConstructor]
    public Correlate()
    {
    }
    
    /// <summary>
    /// The text to write.
    /// </summary>
    [Description("An expression that evaluates to the value to store as the correlation ID")]
    public Input<string> CorrelationId { get; set; } = default!;
    
    protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        var correlationId = context.Get(CorrelationId);
        context.WorkflowExecutionContext.CorrelationId = correlationId;
    }
}