using Elsa.Extensions;
using Elsa.Workflows.Activities.Flowchart.Activities;
using Elsa.Workflows.Activities.Flowchart.Models;

namespace Elsa.Workflows.Activities.Flowchart.Extensions;

public static class ActivityExtensions
{
    public static MergeMode? GetMergeMode(this IActivity activity)
    {
        if (!activity.CustomProperties.TryGetValue("mergeMode", out var value))
            return null;

        // Handle both string and enum values for backwards compatibility
        return value switch
        {
            MergeMode mode => mode,
            string str when Enum.TryParse<MergeMode>(str, true, out var mode) => mode,
            _ => null
        };
    }

    public static void SetMergeMode(this IActivity activity, MergeMode? value)
    {
        if (value == null)
            activity.CustomProperties.Remove("mergeMode");
        else
            activity.CustomProperties["mergeMode"] = value.ToString()!;
    }
    
    public static async Task<MergeMode?> GetMergeModeAsync(this IActivity activity, ActivityExecutionContext context)
    {
        if (activity.Type != "Elsa.FlowJoin")
        {
            return activity.GetMergeMode();
        }
        
        // Handle deprecated FlowJoin activity by evaluating its JoinMode property and mapping it to the appropriate MergeMode equivalent.
        var joinActivityExecutionContext = await context.WorkflowExecutionContext.CreateActivityExecutionContextAsync(activity);
        var joinMode = await joinActivityExecutionContext.EvaluateInputPropertyAsync<FlowJoin, FlowJoinMode>(x => x.Mode);

        return joinMode switch
        {
            FlowJoinMode.WaitAny => MergeMode.Race,
            _ => MergeMode.Converge
        };
    }
}