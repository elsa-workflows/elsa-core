using System.Runtime.CompilerServices;
using System.Text.Json;
using Elsa.Expressions.Models;
using Elsa.Extensions;
using Elsa.Workflows.Activities.Flowchart.Models;
using Elsa.Workflows.Attributes;
using Elsa.Workflows.Contracts;
using Elsa.Workflows.Models;
using Elsa.Workflows.Signals;
using Elsa.Workflows.UIHints;
using JetBrains.Annotations;

namespace Elsa.Workflows.Activities;

/// <summary>
/// Signals the current composite activity to complete itself as a whole.
/// </summary>
[Activity("Elsa", "Composition", "Signals the current composite activity to complete itself as a whole.")]
[PublicAPI]
public class Complete : Activity, ITerminalNode
{
    /// <inheritdoc />
    public Complete([CallerFilePath] string? source = default, [CallerLineNumber] int? line = default) : base(source, line)
    {
    }

    /// <inheritdoc />
    public Complete(IEnumerable<string> outcomes, [CallerFilePath] string? source = default, [CallerLineNumber] int? line = default)
        : this(new Input<object>(outcomes.ToList()), source, line)
    {
    }

    /// <inheritdoc />
    public Complete(Func<ExpressionExecutionContext, ICollection<string>> outcomes, [CallerFilePath] string? source = default, [CallerLineNumber] int? line = default)
        : this(new Input<object>(outcomes), source, line)
    {
    }

    /// <inheritdoc />
    public Complete(Func<ExpressionExecutionContext, string> outcome, [CallerFilePath] string? source = default, [CallerLineNumber] int? line = default)
        : this(context => new[] { outcome(context) }, source, line)
    {
    }

    /// <inheritdoc />
    public Complete(Input<object> outcomes, [CallerFilePath] string? source = default, [CallerLineNumber] int? line = default) : this(source, line)
    {
        Outcomes = outcomes;
    }

    /// <summary>
    /// The outcome or set of outcomes to complete this activity with.
    /// </summary>
    [Input(
        Description = "The outcome or set of outcomes to complete this activity with.",
        UIHint = InputUIHints.OutcomePicker,
        DefaultSyntax = "Object"
    )]
    public Input<object> Outcomes { get; set; } = default!;

    /// <inheritdoc />
    protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        var outcomesValue = Outcomes.GetOrDefault(context);
        var outcomes = InterpretOutcomes(outcomesValue).ToArray();
        
        await context.SendSignalAsync(new CompleteCompositeSignal(new Outcomes(outcomes)));
        await context.CompleteActivityAsync();
    }

    private static IEnumerable<string> InterpretOutcomes(object? outcomesValue)
    {
        switch (outcomesValue)
        {
            case string singleOutcome:
                yield return singleOutcome;
                break;
            case IEnumerable<string> outcomeStrings:
                foreach (var outcome in outcomeStrings)
                    yield return outcome;
                break;
            case IEnumerable<object> outcomeObjects:
            {
                foreach (var outcome in outcomeObjects)
                    yield return outcome.ToString()!;
                break;
            }
            case JsonElement jsonElement:
            {
                if (jsonElement.ValueKind == JsonValueKind.Array)
                {
                    var outcomeArray = jsonElement.EnumerateArray().ToList();
                    foreach (var element in outcomeArray)
                        yield return element.ToString();
                }
                else
                    yield return jsonElement.ToString();

                break;
            }
            default:
                yield return "Done";
                break;
        }
    }
}