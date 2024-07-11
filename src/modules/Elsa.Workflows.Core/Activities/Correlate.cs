using System.ComponentModel;
using System.Runtime.CompilerServices;
using Elsa.Expressions.Models;
using Elsa.Workflows.Attributes;
using Elsa.Workflows.Memory;
using Elsa.Workflows.Models;
using JetBrains.Annotations;

namespace Elsa.Workflows.Activities;

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

    /// <inheritdoc />
    public Correlate(string correlationId, [CallerFilePath] string? source = default, [CallerLineNumber] int? line = default) : base(source, line)
    {
        CorrelationId = new(correlationId);
    }

    /// <inheritdoc />
    public Correlate(Variable<string> correlationId, [CallerFilePath] string? source = default, [CallerLineNumber] int? line = default) : base(source, line)
    {
        CorrelationId = new(correlationId);
    }

    /// <inheritdoc />
    public Correlate(Func<ExpressionExecutionContext, string> correlationId, [CallerFilePath] string? source = default, [CallerLineNumber] int? line = default) : base(source, line)
    {
        CorrelationId = new(correlationId);
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