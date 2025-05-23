using Elsa.Workflows;

namespace Elsa.Resilience.Extensions;

public static class ActivityExecutionExtensions
{
    private const string RetriesAttemptedFlag = "HasRetryAttempts";
    
    public static void SetRetriesAttemptedFlag(this ActivityExecutionContext context)
    {
        var current = context;

        while (current != null)
        {
            current.SetProperty(RetriesAttemptedFlag, true);
            current = current.ParentActivityExecutionContext;
        }
    }
}