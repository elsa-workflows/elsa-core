using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;
using Elsa.Expressions.Models;
using Elsa.Extensions;
using Elsa.Workflows.Core.Attributes;
using Elsa.Workflows.Core.Contracts;
using Elsa.Workflows.Core.Models;
using JetBrains.Annotations;

namespace Elsa.Workflows.Core.Activities;

/// <summary>
/// Evaluate a Boolean condition to determine which activity to execute next.
/// </summary>
[Activity("Elsa", "Branching", "Evaluate a Boolean condition to determine which activity to execute next.")]
[PublicAPI]
public class If : Activity<bool>
{
    /// <inheritdoc />
    [JsonConstructor]
    public If()
    {
    }
    
    /// <inheritdoc />
    public If([CallerFilePath] string? source = default, [CallerLineNumber] int? line = default) : base(source, line)
    {
    }

    /// <inheritdoc />
    public If(Input<bool> condition, [CallerFilePath] string? source = default, [CallerLineNumber] int? line = default) : this(source, line)
    {
        Condition = condition;
    }

    /// <inheritdoc />
    public If(Func<ExpressionExecutionContext, bool> condition, [CallerFilePath] string? source = default, [CallerLineNumber] int? line = default) : this(source, line)
    {
        Condition = new Input<bool>(condition);
    }

    /// <inheritdoc />
    public If(Func<bool> condition, [CallerFilePath] string? source = default, [CallerLineNumber] int? line = default) : this(source, line)
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

    /// <inheritdoc />
    protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        var result = context.Get(Condition);
        var nextActivity = result ? Then : Else;

        context.Set(Result, result);
        await context.ScheduleActivityAsync(nextActivity, OnChildCompleted);
    }

    private async ValueTask OnChildCompleted(ActivityExecutionContext context, ActivityExecutionContext childContext)
    {
        await context.CompleteActivityAsync();
    }
}