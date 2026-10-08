using Elsa.Extensions;
using Elsa.Workflows.Activities.Flowchart.Activities;
using Elsa.Workflows.Activities.Flowchart.Models;

namespace Elsa.Workflows.Activities.Flowchart.Extensions;

public static class ActivityExtensions
{
    extension(IActivity activity)
    {
        public MergeMode? GetMergeMode()
        {
            if (!activity.CustomProperties.TryGetValue("mergeMode", out var value))
                return null;

            // Handle both string and enum values for backwards compatibility
            var result = value switch
            {
                MergeMode mode => mode,
                string str when Enum.TryParse<MergeMode>(str, true, out var mode) => mode,
                _ => (MergeMode?)null
            };

            // Treat MergeMode.None as equivalent to null (no merge mode set)
            return result == MergeMode.None ? null : result;
        }

        public void SetMergeMode(MergeMode? value)
        {
            // Treat MergeMode.None as equivalent to null (no merge mode set)
            if (value == null || value == MergeMode.None)
                activity.CustomProperties.Remove("mergeMode");
            else
                activity.CustomProperties["mergeMode"] = value.ToString()!;
        }

        public async Task<MergeMode?> GetMergeModeAsync(ActivityExecutionContext context)
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
                _ => MergeMode.Merge // WaitAll maps to Merge (wait for all activated branches)
            };
        }
    }
}