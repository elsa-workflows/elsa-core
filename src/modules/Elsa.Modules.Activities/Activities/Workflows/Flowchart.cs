using Elsa.Attributes;
using Elsa.Contracts;
using Elsa.Models;
using Container = Elsa.Activities.Container;

namespace Elsa.Modules.Activities.Activities.Workflows;

public record Connection(IActivity Source, IActivity Target, string? SourcePort, string TargetPort);

[Activity("Elsa", "Workflows", "A flowchart is a collection of activities and connections between them.")]
public class Flowchart : Container
{
    [Node] public IActivity? Start { get; set; }
    public ICollection<Connection> Connections { get; set; } = new List<Connection>();

    protected override void ScheduleChildren(ActivityExecutionContext context)
    {
        if (Start == null!)
            return;

        context.ScheduleActivity(Start, OnChildCompleted);
    }

    private ValueTask OnChildCompleted(ActivityExecutionContext context, ActivityExecutionContext childContext)
    {
        ScheduleChildren(context, childContext.Activity);
        return ValueTask.CompletedTask;
    }

    private void ScheduleChildren(ActivityExecutionContext context, IActivity parent)
    {
        if (parent == null!)
            return;

        var outboundConnections = Connections.Where(x => x.Source == parent).ToList();
        var children = outboundConnections.Select(x => x.Target).ToList();

        context.PostActivities(children, OnChildCompleted);
    }
}