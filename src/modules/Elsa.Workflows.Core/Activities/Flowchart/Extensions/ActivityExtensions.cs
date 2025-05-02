using Elsa.Workflows.Activities.Flowchart.Models;

namespace Elsa.Workflows.Activities.Flowchart.Extensions;

public static class ActivityExtensions
{
    public static JoinKind? GetJoinKind(this IActivity activity)
    {
        activity.CustomProperties.TryGetValue(nameof(JoinKind), out var joinKind);
        return joinKind is JoinKind kind ? kind : null;
    }

    public static void SetJoinKind(this IActivity activity, JoinKind? joinKind)
    {
        if (joinKind == null)
            activity.CustomProperties.Remove(nameof(JoinKind));
        else
            activity.CustomProperties[nameof(JoinKind)] = joinKind;
    }
}