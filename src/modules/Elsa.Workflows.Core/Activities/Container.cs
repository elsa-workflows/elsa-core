using System.Collections.ObjectModel;
using Elsa.Workflows.Core.Attributes;
using Elsa.Workflows.Core.Contracts;
using Elsa.Workflows.Core.Memory;

namespace Elsa.Workflows.Core.Activities;

/// <summary>
/// A base class for activities that control a collection of activities.
/// </summary>
public abstract class Container : Activity, IVariableContainer
{
    /// <inheritdoc />
    protected Container(string? source = default, int? line = default) : base(source, line)
    {
    }

    /// <summary>
    /// The <see cref="IActivity"/>s to execute.
    /// </summary>
    public ICollection<IActivity> Activities { get; set; } = new List<IActivity>();

    /// <summary>
    /// The variables available to this scope.
    /// </summary>
    public ICollection<Variable> Variables { get; set; } = new Collection<Variable>();

    /// <inheritdoc />
    protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        // Ensure variables have names.
        EnsureNames(Variables);

        // Register variables.
        context.ExpressionExecutionContext.Memory.Declare(Variables);

        // Schedule children.
        await ScheduleChildrenAsync(context);
    }

    private void EnsureNames(IEnumerable<Variable> variables)
    {
        var count = 0;

        foreach (var variable in variables)
            if (string.IsNullOrWhiteSpace(variable.Name))
                variable.Name = $"Variable{++count}";
    }

    /// <summary>
    /// Schedule the <see cref="Activities"/> for execution.
    /// </summary>
    protected virtual ValueTask ScheduleChildrenAsync(ActivityExecutionContext context)
    {
        ScheduleChildren(context);
        return ValueTask.CompletedTask;
    }

    /// <summary>
    /// Schedule the <see cref="Activities"/> for execution.
    /// </summary>
    protected virtual void ScheduleChildren(ActivityExecutionContext context)
    {
    }
}