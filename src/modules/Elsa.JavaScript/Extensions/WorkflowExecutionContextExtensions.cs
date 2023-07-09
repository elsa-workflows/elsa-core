using Elsa.Extensions;
using Elsa.Workflows.Core;
using Elsa.Workflows.Core.Contracts;
using Elsa.Workflows.Core.Models;

namespace Elsa.JavaScript.Extensions;

internal static class WorkflowExecutionContextExtensions
{
    public static IEnumerable<(IActivity Activity, ActivityDescriptor ActivityDescriptor)> GetActivitiesWithOutputs(this ActivityExecutionContext activityExecutionContext)
    {
        // Get current container.

        var currentContainerNode = activityExecutionContext.FindParentWithVariableContainer()?.ActivityNode;

        if (currentContainerNode == null)
            yield break;

        // Get all nodes in the current container
        var workflowExecutionContext = activityExecutionContext.WorkflowExecutionContext;
        var containedNodes = workflowExecutionContext.Nodes.Where(x => x.Parents.Contains(currentContainerNode)).Distinct().ToList();

        // Select activities with outputs.
        var activityRegistry = workflowExecutionContext.GetRequiredService<IActivityRegistry>();
        var activitiesWithOutputs = containedNodes.GetActivitiesWithOutputs(activityRegistry);

        foreach (var (activity, activityDescriptor) in activitiesWithOutputs)
            yield return (activity, activityDescriptor);
    }

    public static IEnumerable<(IActivity Activity, ActivityDescriptor ActivityDescriptor)> GetActivitiesWithOutputs(this IEnumerable<ActivityNode> nodes, IActivityRegistry activityRegistry)
    {
        // Select activities with outputs.
        var activitiesWithOutputs =
            from node in nodes
            let activity = node.Activity
            let activityDescriptor = activityRegistry.Find(activity.Type, activity.Version)
            where activityDescriptor.Outputs.Any()
            select (activity, activityDescriptor);

        foreach (var (activity, activityDescriptor) in activitiesWithOutputs)
            yield return (activity, activityDescriptor);
    }
}