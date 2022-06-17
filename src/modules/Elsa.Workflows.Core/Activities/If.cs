using System.Text.Json.Serialization;
using Elsa.Expressions.Models;
using Elsa.Workflows.Core.Attributes;
using Elsa.Workflows.Core.Behaviors;
using Elsa.Workflows.Core.Models;
using Elsa.Workflows.Core.Services;

namespace Elsa.Workflows.Core.Activities;

[Activity("Elsa", "Control Flow", "Evaluate a Boolean condition to determine which path to execute next.")]
public class If : Activity<bool>
{
    [JsonConstructor]
    public If()
    {
        Behaviors.Remove<AutoCompleteBehavior>();
    }

    public If(Input<bool> condition) : this()
    {
        Condition = condition;
    }

    public If(Func<ExpressionExecutionContext, bool> condition) : this()
    {
        Condition = new Input<bool>(condition);
    }

    public If(Func<bool> condition) : this()
    {
        Condition = new Input<bool>(condition);
    }

    /// <summary>
    /// The condition to evaluate.
    /// </summary>
    [Input(UIHint = "single-line")]
    public Input<bool> Condition { get; set; } = new(new Literal<bool>(false));

    /// <summary>
    /// The activity to execute when the condition evaluates to true.
    /// </summary>
    [Port]
    public IActivity? Then { get; set; }

    /// <summary>
    /// The activity to execute when the condition evaluates to false.
    /// </summary>
    [Port]
    public IActivity? Else { get; set; }

    protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        var result = context.Get(Condition);
        var nextNode = result ? Then : Else;

        if (nextNode != null)
        {
            context.ScheduleActivity(nextNode, OnChildCompletedAsync);
        }
        else
        {
            context.Set(Result, result);
            await context.CompleteActivityAsync();
        }
    }

    private async ValueTask OnChildCompletedAsync(ActivityExecutionContext context, ActivityExecutionContext childContext)
    {
        await context.CompleteActivityAsync();
    }
}