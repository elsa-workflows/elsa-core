using Elsa.Resilience.Models;
using Elsa.Workflows;
using Polly.Retry;
using Polly.Telemetry;

namespace Elsa.Resilience.Diagnostics;

public class RetryTelemetryListener : TelemetryListener
{
    public override void Write<TResult, TArgs>(in TelemetryEventArguments<TResult, TArgs> args)
    {
        if (args.Event.EventName == "OnRetry" && args.Arguments is OnRetryArguments<TResult> retryArgs)
        {
            var ctx = args.Context;

            if (!ctx.Properties.TryGetValue<ActivityExecutionContext>(new(nameof(ActivityExecutionContext)), out ActivityExecutionContext? activityExecutionContext))
                return;

            var attempt = retryArgs.AttemptNumber;
            var delay = retryArgs.RetryDelay;
            var outcome = retryArgs.Outcome;
            var record = new RetryAttempt(attempt, delay, outcome.Result, outcome.Exception);
            var records = (List<RetryAttempt>)activityExecutionContext.TransientProperties[RetryAttempt.RetriesKey];
            records.Add(record);
        }
    }
}