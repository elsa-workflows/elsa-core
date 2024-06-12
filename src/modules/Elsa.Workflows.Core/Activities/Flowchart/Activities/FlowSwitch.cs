using System.Runtime.CompilerServices;
using Elsa.Expressions.Contracts;
using Elsa.Expressions.Models;
using Elsa.Workflows.Activities.Flowchart.Attributes;
using Elsa.Workflows.Activities.Flowchart.Models;
using Elsa.Workflows.Attributes;
using Elsa.Workflows.Models;
using Elsa.Workflows.UIHints;
using JetBrains.Annotations;

namespace Elsa.Workflows.Activities.Flowchart.Activities;

/// <summary>
/// Evaluates the specified case conditions and schedules the one that evaluates to <code>true</code>.
/// </summary>
[FlowNode("Default")]
[Activity("Elsa", "Branching", "Evaluate a set of case conditions and schedule the activity for a matching case.", DisplayName = "Switch (flow)")]
[PublicAPI]
public class FlowSwitch : Activity
{
    /// <inheritdoc />
    public FlowSwitch([CallerFilePath] string? source = default, [CallerLineNumber] int? line = default) : base(source, line)
    {
    }

    /// <summary>
    /// The possible cases to evaluate.
    /// </summary>
    [Input(UIHint = "flow-switch-editor")]
    public ICollection<FlowSwitchCase> Cases { get; set; } = new List<FlowSwitchCase>();

    /// <summary>
    /// The switch mode determines whether the first match should be scheduled, or all matches.
    /// </summary>
    [Input(
        Description = "The switch mode determines whether the first match should be scheduled, or all matches.",
        UIHint = InputUIHints.DropDown
    )]
    public Input<SwitchMode> Mode { get; set; } = new(SwitchMode.MatchFirst);

    /// <inheritdoc />
    protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        var matchingCases = (await FindMatchingCasesAsync(context.ExpressionExecutionContext)).ToList();
        var hasAnyMatches = matchingCases.Any();
        var mode = context.Get(Mode);
        var results = mode == SwitchMode.MatchFirst ? hasAnyMatches ? [matchingCases.First()] : Array.Empty<FlowSwitchCase>() : matchingCases.ToArray();
        var outcomes = hasAnyMatches ? results.Select(r => r.Label).ToArray() : ["Default"];

        await context.CompleteActivityAsync(new Outcomes(outcomes));
    }

    private async Task<IEnumerable<FlowSwitchCase>> FindMatchingCasesAsync(ExpressionExecutionContext context)
    {
        var matchingCases = new List<FlowSwitchCase>();
        var expressionEvaluator = context.GetRequiredService<IExpressionEvaluator>();

        foreach (var switchCase in Cases)
        {
            var result = await expressionEvaluator.EvaluateAsync<bool?>(switchCase.Condition, context);

            if (result == true)
            {
                matchingCases.Add(switchCase);
            }
        }

        return matchingCases;
    }
}