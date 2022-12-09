using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;
using Elsa.Expressions.Models;
using Elsa.Workflows.Core.Activities.Flowchart.Models;
using Elsa.Workflows.Core.Attributes;
using Elsa.Workflows.Core.Models;
using Elsa.Workflows.Core.Signals;

namespace Elsa.Workflows.Core.Activities;

/// <summary>
/// Signals the current composite activity to complete itself as a whole.
/// </summary>
[Activity("Elsa", "Primitives", "Signals the current composite activity to complete itself as a whole.")]
public class Complete : Activity
{
    /// <inheritdoc />
    [JsonConstructor]
    public Complete([CallerFilePath] string? source = default, [CallerLineNumber] int? line = default) : base(source, line)
    {
    }

    /// <inheritdoc />
    public Complete(IEnumerable<string> outcomes, [CallerFilePath] string? source = default, [CallerLineNumber] int? line = default)
        : this(new Input<ICollection<string>>(outcomes.ToList()), source, line)
    {
    }

    /// <inheritdoc />
    public Complete(Func<ExpressionExecutionContext, ICollection<string>> outcomes, [CallerFilePath] string? source = default, [CallerLineNumber] int? line = default)
        : this(new Input<ICollection<string>>(outcomes), source, line)
    {
    }

    /// <inheritdoc />
    public Complete(Func<ExpressionExecutionContext, string> outcome, [CallerFilePath] string? source = default, [CallerLineNumber] int? line = default)
        : this(context => new[] { outcome(context) }, source, line)
    {
    }

    /// <inheritdoc />
    public Complete(Input<ICollection<string>> outcomes, [CallerFilePath] string? source = default, [CallerLineNumber] int? line = default) : this(source, line)
    {
        Outcomes = outcomes;
    }

    /// <summary>
    /// The outcome or set of outcomes to complete this activity with.
    /// </summary>
    [Description("The outcome or set of outcomes to complete this activity with.")]
    public Input<ICollection<string>> Outcomes { get; set; } = new(new List<string>());

    /// <inheritdoc />
    protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        var outcomes = Outcomes.Get(context).ToArray();
        await context.SendSignalAsync(new CompleteCompositeSignal(new Outcomes(outcomes)));
    }
}