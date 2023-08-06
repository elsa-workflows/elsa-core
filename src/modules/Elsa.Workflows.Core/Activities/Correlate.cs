using System.ComponentModel;
using System.Runtime.CompilerServices;
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
    public Correlate([CallerFilePath] string? source = default, [CallerLineNumber] int? line = default) : base(source, line)
    {
    }
    
    /// <summary>
    /// The correlation ID to set.
    /// </summary>
    [Description("An expression that evaluates to the value to store as the correlation id")]
    public Input<string> CorrelationId { get; set; } = default!;

    /// <inheritdoc />
    protected override void Execute(ActivityExecutionContext context)
    {
        var correlationId = context.Get(CorrelationId);
        context.WorkflowExecutionContext.CorrelationId = correlationId;
    }
}