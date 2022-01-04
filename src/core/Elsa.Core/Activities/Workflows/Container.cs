using System.Collections.ObjectModel;
using Elsa.Attributes;
using Elsa.Contracts;
using Elsa.Models;

namespace Elsa.Activities.Workflows;

public abstract class Container : Activity, IContainer
{
    protected Container()
    {
    }

    protected Container(params IActivity[] activities) => Activities = activities;
    [Outbound] public ICollection<IActivity> Activities { get; set; } = new List<IActivity>();
    public ICollection<Variable> Variables { get; set; } = new Collection<Variable>();

    protected override void Execute(ActivityExecutionContext context)
    {
        // Register variables.
        context.ExpressionExecutionContext.Register.Declare(Variables);

        // Schedule children.
        ScheduleChildren(context);
    }

    protected abstract void ScheduleChildren(ActivityExecutionContext context);
}