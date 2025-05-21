using Elsa.Workflows;
using Polly;

namespace Elsa.Resilience.StrategySources;

public class RetryEventObserver : IObserver<KeyValuePair<string, object?>>
{
    public void OnNext(KeyValuePair<string, object?> evt)
    {
        // Polly emits events like "Polly.Retry.RetryAsync" for exceptions
        // and           "Polly.ResultRetry.RetryAsync" for result-based retries
        if (evt.Key.EndsWith(".RetryAsync"))
        {
            dynamic payload = evt.Value;
            Exception ex    = payload.Exception;    // null on result-based retries
            object result   = payload.Result;       // default(T) on exception-based retries
            int retryCount  = payload.RetryCount;
            TimeSpan delay  = payload.Delay;
            Context ctx     = payload.Context;

            if (!ctx.TryGetValue(nameof(ActivityExecutionContext), out var value) || value is not ActivityExecutionContext activityExecutionContext)
                return;
            
            // Log retry attempt in activity execution context.
            _ = RegisterRetryAsync(activityExecutionContext, retryCount, delay, ex, result);
        }
    }

    public void OnError(Exception _) { }
    public void OnCompleted() { }
    
    private async Task RegisterRetryAsync(ActivityExecutionContext context, int retryCount, TimeSpan delay, Exception? exception = null, object? result = null)
    {
        // Log retry attempt
    }
}