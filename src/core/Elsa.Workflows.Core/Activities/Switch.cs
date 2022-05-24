using System.Text.Json.Serialization;
using Elsa.Attributes;
using Elsa.Expressions;
using Elsa.Models;
using Elsa.Services;

namespace Elsa.Activities;

/// <summary>
/// The Switch activity is an approximation of the `switch` construct in C#.
/// When a case evaluates to true, the associated activity is then scheduled for execution.
/// </summary>
[Activity("Elsa", "Control Flow", "Evaluate a set of case conditions and schedule the activity for a matching case.")]
public class Switch : Activity
{
    [Input(UIHint = "switch-editor")] public ICollection<SwitchCase> Cases { get; set; } = new List<SwitchCase>();
    public IActivity? Default { get; set; }

    protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        var matchingCase = await FindMatchingCaseAsync(context.ExpressionExecutionContext);

        if (matchingCase != null)
        {
            if (matchingCase.Activity != null)
                context.ScheduleActivity(matchingCase.Activity);
            return;
        }

        if (Default != null)
            context.ScheduleActivity(Default);
    }

    private async Task<SwitchCase?> FindMatchingCaseAsync(ExpressionExecutionContext context)
    {
        var expressionEvaluator = context.GetRequiredService<IExpressionEvaluator>();

        foreach (var switchCase in Cases)
        {
            var result = await expressionEvaluator.EvaluateAsync<bool>(switchCase.Condition, context);

            if (result)
                return switchCase;
        }

        return null;
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

    public SwitchCase(string label, DelegateReference<bool> condition, IActivity activity) : this(label, new DelegateExpression(condition), activity)
    {
    }

    public SwitchCase(string label, Func<ExpressionExecutionContext, ValueTask<bool>> condition, IActivity activity) : this(label, new DelegateReference<bool>(condition), activity)
    {
    }

    public SwitchCase(string label, Func<ValueTask<bool>> condition, IActivity activity) : this(label, new DelegateReference<bool>(condition), activity)
    {
    }

    public SwitchCase(string label, Func<ExpressionExecutionContext, bool> condition, IActivity activity) : this(label, new DelegateReference<bool>(condition), activity)
    {
    }

    public SwitchCase(string label, Func<bool> condition, IActivity activity) : this(label, new DelegateReference<bool>(condition), activity)
    {
    }

    public string Label { get; set; } = default!;
    public IExpression Condition { get; set; } = new LiteralExpression(false);
    public IActivity? Activity { get; set; }
}