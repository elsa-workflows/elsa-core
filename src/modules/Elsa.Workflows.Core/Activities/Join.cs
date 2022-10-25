using System.Collections.Generic;
using System.Linq;
using Elsa.Common.Extensions;
using Elsa.Workflows.Core.Attributes;
using Elsa.Workflows.Core.Models;

namespace Elsa.Workflows.Core.Activities;

public enum JoinMode
{
    WaitAny,
    WaitAll
}

public class Join : Activity
{
    public Join(JoinMode joinMode = Activities.JoinMode.WaitAny)
    {
        JoinMode = new Input<JoinMode>(joinMode);
    }

    [Input] public Input<JoinMode> JoinMode { get; set; }

    protected override void Execute(ActivityExecutionContext context)
    {
        var joinMode = context.Get(JoinMode);

        switch (joinMode)
        {
            case Activities.JoinMode.WaitAny:
            {
                // Remove any and all bookmarks from other branches.
                RemoveBookmarks(context);
            }
                break;
            case Activities.JoinMode.WaitAll:
            {
                var workflowExecutionContext = context.WorkflowExecutionContext;

                // Record last executed activity ID.
                var lastActivityId = context.WorkflowExecutionContext.ExecutionLog.LastOrDefault(x => x.EventName == WorkflowExecutionLogEventNames.Executed)?.ActivityId;

                if (lastActivityId != null)
                {
                    var recordedActivitiesKey = $"{Id}:RecordedActivities";
                    var recordedActivityIds = workflowExecutionContext.UpdateProperty<HashSet<string>>(recordedActivitiesKey, set =>
                    {
                        set ??= new HashSet<string>();
                        set.Add(lastActivityId);
                        return set;
                    });

                    var inboundActivityIds = context.ActivityNode.Parents.Select(x => x.Activity.Id).ToHashSet();
                    var allSet = inboundActivityIds.All(x => recordedActivityIds.Contains(x));

                    if (!allSet)
                    {
                        // If not all inbound activities have executed yet, instruct engine to not complete this activity.
                        context.PreventContinuation();
                    }
                    else
                    {
                        // If all inbound activities have executed, clean global state.
                        workflowExecutionContext.Properties.Remove(recordedActivitiesKey);
                    }
                }

                break;
            }
        }
    }

    private void RemoveBookmarks(ActivityExecutionContext context)
    {
        var workflowExecutionContext = context.WorkflowExecutionContext;

        // Remove all bookmarks of ancestors and siblings.
        var joinNode = context.ActivityNode;
        var ancestors = joinNode.Ancestors().Select(x => x.Activity.Id);
        var siblingsAndCousins = joinNode.SiblingsAndCousins().Select(x => x.Activity.Id);
        var activitiesInPath = ancestors.Concat(siblingsAndCousins).ToHashSet();
        
        // Remove any bookmarks of any ancestors.
        workflowExecutionContext.Bookmarks.RemoveWhere(x => activitiesInPath.Contains(x.ActivityId));
    }
}