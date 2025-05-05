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
    
    public static MergeMode? GetMergeMode(this IActivity activity)
    {
        activity.CustomProperties.TryGetValue("mergeMode", out var mergeModeString);
        return Enum.TryParse<MergeMode>((string?)mergeModeString, true, out var mergeMode) ? mergeMode : null;
    }

    public static void SetMergeMode(this IActivity activity, MergeMode? value)
    {
        if (value == null)
            activity.CustomProperties.Remove("mergeMode");
        else
            activity.CustomProperties["mergeMode"] = value;
    }
}