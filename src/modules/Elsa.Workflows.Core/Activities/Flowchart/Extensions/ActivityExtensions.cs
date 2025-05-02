using Elsa.Workflows.Activities.Flowchart.Models;

namespace Elsa.Workflows.Activities.Flowchart.Extensions;

public static class ActivityExtensions
{
    public static JoinKind? GetJoinKind(this IActivity activity)
    {
        activity.CustomProperties.TryGetValue("joinKind", out var joinKindString);
        return Enum.TryParse<JoinKind>((string?)joinKindString, true, out var joinKind) ? joinKind : null;
    }

    public static void SetJoinKind(this IActivity activity, JoinKind? joinKind)
    {
        if (joinKind == null)
            activity.CustomProperties.Remove("joinKind");
        else
            activity.CustomProperties["joinKind"] = joinKind;
    }
}