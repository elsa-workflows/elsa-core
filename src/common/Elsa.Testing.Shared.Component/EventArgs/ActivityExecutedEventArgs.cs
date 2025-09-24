using Elsa.Workflows;

namespace Elsa.Testing.Shared;

public class ActivityExecutedEventArgs(ActivityExecutionContext activityExecutionContext) : EventArgs
{
    public ActivityExecutionContext ActivityExecutionContext { get; } = activityExecutionContext;
}