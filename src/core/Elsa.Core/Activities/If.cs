using System.Text.Json.Serialization;
using Elsa.Attributes;
using Elsa.Contracts;
using Elsa.Models;

namespace Elsa.Activities;

[Activity("Elsa", "Control Flow", "Evaluate a Boolean condition to determine which path to execute next.")]
public class If : Activity
{
    [JsonConstructor]
    public If()
    {
    }

    public If(Input<bool> condition) => Condition = condition;
    public If(Func<ExpressionExecutionContext, bool> condition) => Condition = new Input<bool>(condition);
    public If(Func<bool> condition) => Condition = new Input<bool>(condition);

    /// <summary>
    /// The condition to evaluate.
    /// </summary>
    [Input] public Input<bool> Condition { get; set; } = new(new Literal<bool>(false));
    
    /// <summary>
    /// The activity to execute when the condition evaluates to true.
    /// </summary>
    [Outbound] public IActivity? Then { get; set; }
    
    /// <summary>
    /// The activity to execute when the condition evaluates to false.
    /// </summary>
    [Outbound] public IActivity? Else { get; set; }
        
    protected override void Execute(ActivityExecutionContext context)
    {
        var result = context.Get(Condition);
        var nextNode = result ? Then : Else;

        if (nextNode != null)
            context.PostActivity(nextNode, OnChildCompletedAsync);
    }

    private ValueTask OnChildCompletedAsync(ActivityExecutionContext context, ActivityExecutionContext childContext) => ValueTask.CompletedTask;
}