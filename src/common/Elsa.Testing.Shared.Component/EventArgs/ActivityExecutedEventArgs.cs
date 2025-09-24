using Elsa.Workflows;

namespace Elsa.Testing.Shared.EventArgs;

public class ActivityExecutedEventArgs(ActivityExecutionContext activityExecutionContext) : System.EventArgs
{
    public ActivityExecutionContext ActivityExecutionContext { get; } = activityExecutionContext;
}