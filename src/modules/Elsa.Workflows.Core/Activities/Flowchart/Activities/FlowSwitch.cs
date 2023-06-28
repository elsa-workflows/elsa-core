using Elsa.Expressions;
using Elsa.Expressions.Contracts;
using Elsa.Expressions.Models;
using Elsa.Extensions;
using Elsa.Workflows.Core.Activities.Flowchart.Attributes;
using Elsa.Workflows.Core.Activities.Flowchart.Models;
using Elsa.Workflows.Core.Attributes;
using Elsa.Workflows.Core.Models;
using JetBrains.Annotations;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

namespace Elsa.Workflows.Core.Activities.Flowchart.Activities;

/// <summary>
/// Evaluates the specified case conditions and schedules the one that evaluates to <code>true</code>.
/// </summary>
[FlowNode("Default")]
[Activity("Elsa", "Branching", "Evaluate a set of case conditions and schedule the activity for a matching case.")]
[PublicAPI]
public class FlowSwitch : Activity
{
    /// <inheritdoc />
    [JsonConstructor]
    public FlowSwitch()
    {
    }

    /// <inheritdoc />
    public FlowSwitch([CallerFilePath] string? source = default, [CallerLineNumber] int? line = default) : base(source, line)
    {
    }

    [Input(UIHint = "flow-switch-editor")] public ICollection<FlowSwitchCase> Cases { get; set; } = new List<FlowSwitchCase>();

    /// <summary>
    /// The switch mode determines whether the first match should be scheduled, or all matches.
    /// </summary>
    [Input(Description = "The switch mode determines whether the first match should be scheduled, or all matches.")]
    public Input<SwitchMode> Mode { get; set; } = new(SwitchMode.MatchFirst);

    /// <inheritdoc />
    protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        var matchingCases = (await FindMatchingCasesAsync(context.ExpressionExecutionContext)).ToList();
        var hasAnyMatches = matchingCases.Any();
        var mode = context.Get(Mode);
        var results = mode == SwitchMode.MatchFirst ? hasAnyMatches ? new[] { matchingCases.First() } : Array.Empty<FlowSwitchCase>() : matchingCases.ToArray();
        var outcomes = hasAnyMatches ? results.Select(r => r.Label).ToArray() : new[] { "Default" };

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

/// <summary>
/// Represents an individual case of the <see cref="FlowSwitch"/> activity.
/// </summary>
public class FlowSwitchCase
{
    [JsonConstructor]
    public FlowSwitchCase()
    {
    }

    public FlowSwitchCase(string label, IExpression condition)
    {
        Label = label;
        Condition = condition;
    }

    public FlowSwitchCase(string label, DelegateBlockReference<bool> condition) : this(label, new DelegateExpression(condition))
    {
    }

    public FlowSwitchCase(string label, Func<ExpressionExecutionContext, ValueTask<bool>> condition) : this(label, new DelegateBlockReference<bool>(condition))
    {
    }

    public FlowSwitchCase(string label, Func<ValueTask<bool>> condition) : this(label, new DelegateBlockReference<bool>(condition))
    {
    }

    public FlowSwitchCase(string label, Func<ExpressionExecutionContext, bool> condition) : this(label, new DelegateBlockReference<bool>(condition))
    {
    }

    public FlowSwitchCase(string label, Func<bool> condition) : this(label, new DelegateBlockReference<bool>(condition))
    {
    }

    public string Label { get; set; } = default!;
    public IExpression Condition { get; set; } = new LiteralExpression(false);
}