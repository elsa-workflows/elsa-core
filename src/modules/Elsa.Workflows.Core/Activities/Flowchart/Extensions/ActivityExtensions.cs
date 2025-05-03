using Elsa.Workflows.Activities.Flowchart.Models;

namespace Elsa.Workflows.Activities.Flowchart.Extensions;

public static class ActivityExtensions
{
    public static FlowJoinMode? GetJoinMode(this IActivity activity)
    {
        activity.CustomProperties.TryGetValue("joinMode", out var joinModeString);
        return Enum.TryParse<FlowJoinMode>((string?)joinModeString, true, out var joinMode) ? joinMode : null;
    }

    public static void SetJoinMode(this IActivity activity, FlowJoinMode? value)
    {
        if (value == null)
            activity.CustomProperties.Remove("joinMode");
        else
            activity.CustomProperties["joinMode"] = value;
    }
}