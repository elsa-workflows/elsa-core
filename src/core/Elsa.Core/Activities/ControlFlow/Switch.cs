using Elsa.Attributes;
using Elsa.Contracts;
using Elsa.Expressions;
using Elsa.Models;

namespace Elsa.Activities.ControlFlow;

/// <summary>
/// The Switch activity is an approximation of the `switch` construct in C#.
/// When a case evaluates to true, the associated activity is then scheduled for execution.
/// </summary>
public class Switch : Activity
{
    [Input(UIHint = UIHints.SwitchEditor)] public ICollection<SwitchCase> Cases { get; set; } = new List<SwitchCase>();
    public IActivity? Default { get; set; }

    protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        var matchingCase = await FindMatchingCaseAsync(context.ExpressionExecutionContext);

        if (matchingCase != null)
        {
            if (matchingCase.Activity != null)
                context.SubmitActivity(matchingCase.Activity);
            return;
        }

        if (Default != null)
            context.SubmitActivity(Default);
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
    // ReSharper disable once EmptyConstructor
    public SwitchCase()
    {
    }

    public string Label { get; set; } = default!;
    public IExpression Condition { get; set; } = new LiteralExpression(false);
    public IActivity? Activity { get; set; }
}