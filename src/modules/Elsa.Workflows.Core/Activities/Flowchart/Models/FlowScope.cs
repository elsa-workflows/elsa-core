using System.Text.Json.Serialization;
using Elsa.Workflows.Core.Contracts;

namespace Elsa.Workflows.Core.Activities.Flowchart.Models;

internal class FlowScope
{
    [JsonConstructor]
    public FlowScope()
    {
    }

    public FlowScope(string ownerActivityId)
    {
        OwnerActivityId = ownerActivityId;
        Activities.Add(ownerActivityId, new ActivityFlowState(ownerActivityId, 1));
    }

    /// <summary>
    /// The activity from which the scope was created.
    /// </summary>
    public string OwnerActivityId { get; set; } = default!;

    /// <summary>
    /// A list of scheduled activity IDs and a flag whether they executed or not.
    /// </summary>
    public IDictionary<string, ActivityFlowState> Activities { get; set; } =
        new Dictionary<string, ActivityFlowState>();

    public void AddActivities(IEnumerable<IActivity> activities, long executionCount = 0)
    {
        foreach (var activity in activities)
            AddActivity(activity, executionCount);
    }
    
    public void AddActivity(IActivity activity, long executionCount = 0) => EnsureActivity(activity, executionCount);

    public ActivityFlowState EnsureActivity(IActivity activity, long executionCount = 0)
    {
        if (Activities.ContainsKey(activity.Id)) 
            return Activities[activity.Id];
        
        var state = new ActivityFlowState(activity.Id, executionCount);
        Activities.Add(activity.Id, state);
        return state;

    }
    
    public void RegisterActivityExecution(IActivity activity)
    {
        var state = Activities.TryGetValue(activity.Id, out var s) ? s : default;

        if (state == null)
        {
            state = new ActivityFlowState(activity.Id);
            Activities[activity.Id] = state;
        }

        state.ExecutionCount++;
    }

    /// <summary>
    /// Return a list excluding any activities that already executed.
    /// </summary>
    public IEnumerable<IActivity> ExcludeExecutedActivities(IEnumerable<IActivity> activities) =>
        activities.Where(x => !Activities.ContainsKey(x.Id) || Activities[x.Id].ExecutionCount == 0);

    public bool HasPendingActivities()
    {
        var sample = Activities.Values.First().ExecutionCount;
        return Activities.Values.Any(x => x.ExecutionCount != sample);
    }

    public long GetExecutionCount(IActivity activity) => Activities.ContainsKey(activity.Id) ? Activities[activity.Id].ExecutionCount : 0;

    public void Clear() => Activities.Clear();
}