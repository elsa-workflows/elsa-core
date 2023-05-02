using System.ComponentModel;
using System.Text.Json.Serialization;
using Elsa.Workflows.Core.Attributes;
using Elsa.Workflows.Core.Models;
using JetBrains.Annotations;

namespace Elsa.Workflows.Core.Activities;

/// <summary>
/// Set the CorrelationId of the workflow to a given value.
/// </summary>
[Activity("Elsa", "Primitives", "Set the CorrelationId of the workflow to a given value.")]
[PublicAPI]
public class Correlate : CodeActivity
{
    /// <inheritdoc />
    [JsonConstructor]
    public Correlate()
    {
    }
    
    /// <summary>
    /// The correlation ID to set.
    /// </summary>
    [Description("An expression that evaluates to the value to store as the correlation id")]
    public Input<string> CorrelationId { get; set; } = default!;
    
    protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        var correlationId = context.Get(CorrelationId);
        context.WorkflowExecutionContext.CorrelationId = correlationId;
    }
}