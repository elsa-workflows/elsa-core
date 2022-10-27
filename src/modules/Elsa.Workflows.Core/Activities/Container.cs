using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Elsa.Workflows.Core.Attributes;
using Elsa.Workflows.Core.Behaviors;
using Elsa.Workflows.Core.Models;
using Elsa.Workflows.Core.Services;

namespace Elsa.Workflows.Core.Activities;

/// <summary>
/// A base class for activities that control a collection of activities.
/// </summary>
public abstract class Container : Activity, IContainer
{
    protected Container()
    {
        Behaviors.Remove<AutoCompleteBehavior>();
    }

    protected Container(params IActivity[] activities) : this()
    {
        Activities = activities;
    }

    protected Container(ICollection<Variable> variables, params IActivity[] activities) : this(activities)
    {
        Variables = variables;
    }

    [Port]public ICollection<IActivity> Activities { get; set; } = new HashSet<IActivity>();
    public ICollection<Variable> Variables { get; set; } = new Collection<Variable>();

    protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        // Register variables.
        context.ExpressionExecutionContext.Memory.Declare(Variables);

        // Schedule children.
        await ScheduleChildrenAsync(context);
    }

    protected virtual ValueTask ScheduleChildrenAsync(ActivityExecutionContext context)
    {
        ScheduleChildren(context);
        return ValueTask.CompletedTask;
    }

    protected virtual void ScheduleChildren(ActivityExecutionContext context)
    {
    }
}