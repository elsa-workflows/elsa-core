using Elsa.Testing.Framework.Models;
using Elsa.Workflows;

namespace Elsa.Testing.Framework.Services;

public class ActivityTracer
{
    private readonly ICollection<ActivityExecutionTrace> _traces = new List<ActivityExecutionTrace>();
    public void TraceActivityExecution(ActivityExecutionContext context)
    {
        var trace = ActivityExecutionTrace.Create(context);
        _traces.Add(trace);
    }

    public bool ContainsActivityExecution(string activityId)
    {
        return _traces.Any(x => x.ActivityId == activityId);
    }
}