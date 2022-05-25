using Elsa.Workflows.Core.Attributes;
using Elsa.Workflows.Core.Models;
using Elsa.Workflows.Core.Services;
using Elsa.Workflows.Core.Signals;

namespace Elsa.Workflows.Core.Activities;

public record Connection(IActivity Source, IActivity Target, string? SourcePort, string TargetPort);

[Activity("Elsa", "Workflows", "A flowchart is a collection of activities and connections between them.")]
public class Flowchart : Container
{
    public Flowchart()
    {
        OnSignalReceived<ActivityCompleted>(OnDescendantCompleted);
    }

    [Node] public IActivity? Start { get; set; }
    public ICollection<Connection> Connections { get; set; } = new List<Connection>();

    protected override void ScheduleChildren(ActivityExecutionContext context)
    {
        if (Start == null!)
            return;

        context.ScheduleActivity(Start);
    }
    
    private void OnDescendantCompleted(ActivityCompleted signal, SignalContext context)
    {
        ScheduleChildren(context.ActivityExecutionContext, context.SourceActivityExecutionContext.Activity);
    }

    private void ScheduleChildren(ActivityExecutionContext context, IActivity parent)
    {
        if (parent == null!)
            return;

        var outboundConnections = Connections.Where(x => x.Source == parent).ToList();
        var children = outboundConnections.Select(x => x.Target).ToList();

        context.ScheduleActivities(children);
    }
}