using System.Text.Json.Serialization;
using Elsa.Expressions;
using Elsa.Expressions.Models;
using Elsa.Expressions.Services;
using Elsa.Workflows.Core.Attributes;
using Elsa.Workflows.Core.Models;
using Elsa.Workflows.Core.Services;

namespace Elsa.Workflows.Core.Activities;

/// <summary>
/// The Switch activity is an approximation of the `switch` construct in C#.
/// When a case evaluates to true, the associated activity is then scheduled for execution.
/// </summary>
[Activity("Elsa", "Control Flow", "Evaluate a set of case conditions and schedule the activity for a matching case.")]
public class Switch : ActivityBase
{
    /// <summary>
    /// The value to switch on.
    /// </summary>
    [Input(Description = "The value to switch on.")]
    public Input<object> Expression { get; set; } = default!;

    /// <summary>
    /// The value to switch on, made available as output for capturing.
    /// </summary>
    public Output<object>? Output { get; set; }
    
    [Input(UIHint = "switch-editor")] public ICollection<SwitchCase> Cases { get; set; } = new List<SwitchCase>();
    public IActivity? Default { get; set; }

    protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        context.Set(Output, Expression);
        var matchingCase = await FindMatchingCaseAsync(context.ExpressionExecutionContext);

        if (matchingCase != null)
        {
            if (matchingCase.Activity != null)
                await context.ScheduleActivityAsync(matchingCase.Activity, OnChildActivityCompletedAsync);
            return;
        }

        if (Default != null)
            await context.ScheduleActivityAsync(Default, OnChildActivityCompletedAsync);
    }

    private async Task<SwitchCase?> FindMatchingCaseAsync(ExpressionExecutionContext context)
    {
        var expressionEvaluator = context.GetRequiredService<IExpressionEvaluator>();

        foreach (var switchCase in Cases)
        {
            var result = await expressionEvaluator.EvaluateAsync<bool?>(switchCase.Condition, context);

            if (result == true)
                return switchCase;
        }

        return null;
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
    [JsonConstructor]
    public SwitchCase()
    {
    }

    public SwitchCase(string label, IExpression condition, IActivity activity)
    {
        Label = label;
        Condition = condition;
        Activity = activity;
    }

    public SwitchCase(string label, DelegateBlockReference<bool> condition, IActivity activity) : this(label, new DelegateExpression(condition), activity)
    {
    }

    public SwitchCase(string label, Func<ExpressionExecutionContext, ValueTask<bool>> condition, IActivity activity) : this(label, new DelegateBlockReference<bool>(condition), activity)
    {
    }

    public SwitchCase(string label, Func<ValueTask<bool>> condition, IActivity activity) : this(label, new DelegateBlockReference<bool>(condition), activity)
    {
    }

    public SwitchCase(string label, Func<ExpressionExecutionContext, bool> condition, IActivity activity) : this(label, new DelegateBlockReference<bool>(condition), activity)
    {
    }

    public SwitchCase(string label, Func<bool> condition, IActivity activity) : this(label, new DelegateBlockReference<bool>(condition), activity)
    {
    }

    public string Label { get; set; } = default!;
    public IExpression Condition { get; set; } = new LiteralExpression(false);
    public IActivity? Activity { get; set; }
}