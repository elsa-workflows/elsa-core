using System.Text.Json.Serialization;
using Elsa.Workflows.Core.Services;

namespace Elsa.Workflows.Core.Activities.Flowchart.Models;

public class FlowScope
{
    [JsonConstructor]
    public FlowScope()
    {
    }

    public FlowScope(string ownerActivityId)
    {
        OwnerActivityId = ownerActivityId;
        Activities.Add(ownerActivityId, new ActivityFlowState(ownerActivityId, true));
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

    public void AddActivities(IEnumerable<IActivity> activities, bool executed = false)
    {
        foreach (var activity in activities)
            AddActivity(activity, executed);
    }
    
    public void AddActivity(IActivity activity, bool executed = false)
    {
        if (!Activities.ContainsKey(activity.Id))
            Activities.Add(activity.Id, new ActivityFlowState(activity.Id, executed));
    }
    
    public void RegisterActivityExecution(IActivity activity)
    {
        Activities[activity.Id] = new ActivityFlowState(activity.Id, true);
    }

    public void MarkAsExecuted(IActivity activity)
    {
        if (Activities.TryGetValue(activity.Id, out var state))
            state.HasExecuted = true;
    }

    /// <summary>
    /// Return a list excluding any activities that already executed.
    /// </summary>
    public IEnumerable<IActivity> ExcludeExecutedActivities(IEnumerable<IActivity> activities) =>
        activities.Where(x => !Activities.ContainsKey(x.Id) || !Activities[x.Id].HasExecuted);

    public bool HasPendingActivities() => Activities.Values.Any(x => !x.HasExecuted);

    public bool HaveExecuted(IEnumerable<IActivity> activities) => activities.All(activity =>
        Activities.TryGetValue(activity.Id, out var f) && f.HasExecuted);
}