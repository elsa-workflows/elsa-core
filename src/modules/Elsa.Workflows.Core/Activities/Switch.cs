using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;
using Elsa.Expressions;
using Elsa.Expressions.Contracts;
using Elsa.Expressions.Models;
using Elsa.Extensions;
using Elsa.Workflows.Core.Attributes;
using Elsa.Workflows.Core.Contracts;
using Elsa.Workflows.Core.Models;
using JetBrains.Annotations;

namespace Elsa.Workflows.Core.Activities;

/// <summary>
/// The Switch activity is an approximation of the `switch` construct in C#.
/// When a case evaluates to true, the associated activity is then scheduled for execution.
/// </summary>
[Activity("Elsa", "Branching", "Evaluate a set of case conditions and schedule the activity for a matching case.")]
[PublicAPI]
public class Switch : Activity
{
    /// <inheritdoc />
    [JsonConstructor]
    public Switch()
    {
    }

    /// <inheritdoc />
    public Switch([CallerFilePath] string? source = default, [CallerLineNumber] int? line = default) : base(source, line)
    {
    }

    /// <summary>
    /// The value to switch on, made available as output for capturing.
    /// </summary>
    public Output<object>? Output { get; set; }

    /// <summary>
    /// The cases to evaluate.
    /// </summary>
    [Input(
        Description = "The cases to evaluate.",
        UIHint = "switch-editor"
    )]
    public ICollection<SwitchCase> Cases { get; set; } = new List<SwitchCase>();

    /// <summary>
    /// The switch mode determines whether the first match should be scheduled, or all matches.
    /// </summary>
    [Input(Description = "The switch mode determines whether the first match should be scheduled, or all matches.")]
    public Input<SwitchMode> Mode { get; set; } = new(SwitchMode.MatchFirst);

    /// <summary>
    /// The default activity to schedule when no case matches.
    /// </summary>
    [Port] public IActivity? Default { get; set; }

    /// <inheritdoc />
    protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        var matchingCases = (await FindMatchingCasesAsync(context.ExpressionExecutionContext)).ToList();
        var hasAnyMatches = matchingCases.Any();
        var mode = context.Get(Mode);
        var results = mode == SwitchMode.MatchFirst ? hasAnyMatches ? new[] { matchingCases.First() } : Array.Empty<SwitchCase>() : matchingCases.ToArray();

        if (hasAnyMatches)
        {
            foreach (var result in results)
            {
                await context.ScheduleActivityAsync(result.Activity, OnChildActivityCompletedAsync);
            }

            return;
        }

        await context.ScheduleActivityAsync(Default, OnChildActivityCompletedAsync);
    }

    private async Task<IEnumerable<SwitchCase>> FindMatchingCasesAsync(ExpressionExecutionContext context)
    {
        var matchingCases = new List<SwitchCase>();
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

    private async ValueTask OnChildActivityCompletedAsync(ActivityExecutionContext context, ActivityExecutionContext childContext)
    {
        await context.CompleteActivityAsync();
    }
}

/// <summary>
/// Represents an individual case of the <see cref="Switch"/> activity.
/// </summary>
public class SwitchCase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SwitchCase"/> class.
    /// </summary>
    [JsonConstructor]
    public SwitchCase()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SwitchCase"/> class.
    /// </summary>
    /// <param name="label">The label of the case.</param>
    /// <param name="condition">The condition to evaluate.</param>
    /// <param name="activity">The activity to schedule when the condition evaluates to true.</param>
    public SwitchCase(string label, IExpression condition, IActivity activity)
    {
        Label = label;
        Condition = condition;
        Activity = activity;
    }

    /// <inheritdoc />
    public SwitchCase(string label, DelegateBlockReference<bool> condition, IActivity activity) : this(label, new DelegateExpression(condition), activity)
    {
    }

    /// <inheritdoc />
    public SwitchCase(string label, Func<ExpressionExecutionContext, ValueTask<bool>> condition, IActivity activity) : this(label, new DelegateBlockReference<bool>(condition), activity)
    {
    }

    /// <inheritdoc />
    public SwitchCase(string label, Func<ValueTask<bool>> condition, IActivity activity) : this(label, new DelegateBlockReference<bool>(condition), activity)
    {
    }

    /// <inheritdoc />
    public SwitchCase(string label, Func<ExpressionExecutionContext, bool> condition, IActivity activity) : this(label, new DelegateBlockReference<bool>(condition), activity)
    {
    }

    /// <inheritdoc />
    public SwitchCase(string label, Func<bool> condition, IActivity activity) : this(label, new DelegateBlockReference<bool>(condition), activity)
    {
    }

    /// <summary>
    /// The label of the case.
    /// </summary>
    public string Label { get; set; } = default!;

    /// <summary>
    /// The condition to evaluate.
    /// </summary>
    public IExpression Condition { get; set; } = new LiteralExpression(false);

    /// <summary>
    /// The activity to schedule when the condition evaluates to true.
    /// </summary>
    public IActivity? Activity { get; set; }
}