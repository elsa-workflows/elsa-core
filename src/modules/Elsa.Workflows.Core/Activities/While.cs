using System.Runtime.CompilerServices;
using Elsa.Expressions.Models;
using Elsa.Extensions;
using Elsa.Workflows.Core.Attributes;
using Elsa.Workflows.Core.Behaviors;
using Elsa.Workflows.Core.Contracts;
using Elsa.Workflows.Core.Models;
using JetBrains.Annotations;

namespace Elsa.Workflows.Core.Activities;

/// <summary>
/// Execute an activity while a given condition evaluates to true.
/// </summary>
[Activity("Elsa", "Looping", "Execute an activity while a given condition evaluates to true.")]
[PublicAPI]
public class While : Activity
{
    /// <summary>
    /// Creates a <see cref="While"/> activity that loops forever.
    /// </summary>
    public static While True(IActivity body) => new(body)
    {
        Condition = new Input<bool>(true)
    };

    /// <inheritdoc />
    public While([CallerFilePath] string? source = default, [CallerLineNumber] int? line = default) : base(source, line)
    {
        Behaviors.Add<BreakBehavior>(this);
    }

    /// <inheritdoc />
    public While(IActivity? body = default, [CallerFilePath] string? source = default, [CallerLineNumber] int? line = default) : this(source, line)
    {
        Body = body;
    }

    /// <inheritdoc />
    public While(Input<bool> condition, IActivity? body = default, [CallerFilePath] string? source = default, [CallerLineNumber] int? line = default) : this(body, source, line)
    {
        Condition = condition;
    }

    /// <inheritdoc />
    public While(Func<ExpressionExecutionContext, ValueTask<bool>> condition, IActivity? body = default, [CallerFilePath] string? source = default, [CallerLineNumber] int? line = default)
        : this(new Input<bool>(condition), body, source, line)
    {
    }

    /// <inheritdoc />
    public While(Func<ExpressionExecutionContext, bool> condition, IActivity? body = default, [CallerFilePath] string? source = default, [CallerLineNumber] int? line = default)
        : this(new Input<bool>(condition), body, source, line)
    {
    }

    /// <inheritdoc />
    public While(Func<ValueTask<bool>> condition, IActivity? body = default, [CallerFilePath] string? source = default, [CallerLineNumber] int? line = default)
        : this(new Input<bool>(condition), body, source, line)
    {
    }

    /// <inheritdoc />
    public While(Func<bool> condition, IActivity? body = default, [CallerFilePath] string? source = default, [CallerLineNumber] int? line = default)
        : this(new Input<bool>(condition), body, source, line)
    {
    }

    /// <summary>
    /// The condition to evaluate.
    /// </summary>
    [Input(AutoEvaluate = false)]
    public Input<bool> Condition { get; set; } = new(false);

    /// <summary>
    /// The <see cref="IActivity"/> to execute on every iteration.
    /// </summary>
    [Port]
    public IActivity? Body { get; set; }

    /// <inheritdoc />
    protected override async ValueTask ExecuteAsync(ActivityExecutionContext context) => await HandleIterationAsync(context);

    private async ValueTask OnBodyCompleted(ActivityCompletedContext context)
    {
        await HandleIterationAsync(context.TargetContext);
    }

    private async ValueTask HandleIterationAsync(ActivityExecutionContext context)
    {
        var isBreaking = context.GetIsBreaking();
        var loop = !isBreaking && await context.EvaluateInputPropertyAsync<While, bool>(x => x.Condition);

        if (loop)
            await context.ScheduleActivityAsync(Body, OnBodyCompleted);
        else
            await context.CompleteActivityAsync();
    }
}