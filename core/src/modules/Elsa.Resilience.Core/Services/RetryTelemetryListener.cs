using Elsa.Resilience.Models;
using Elsa.Workflows;
using Polly.Retry;
using Polly.Telemetry;

namespace Elsa.Resilience;

public class RetryTelemetryListener : TelemetryListener
{
    public override void Write<TResult, TArgs>(in TelemetryEventArguments<TResult, TArgs> args)
    {
        if (args.Event.EventName != "OnRetry" || args.Arguments is not OnRetryArguments<TResult> retryArgs)
            return;

        var ctx = args.Context;

        if (!ctx.Properties.TryGetValue<ActivityExecutionContext>(new(nameof(ActivityExecutionContext)), out var activityExecutionContext))
            return;

        var attempt = retryArgs.AttemptNumber;
        var delay = retryArgs.RetryDelay;
        var outcome = retryArgs.Outcome;
        var record = new RetryAttempt(activityExecutionContext, attempt, delay, outcome.Result, outcome.Exception);
        var records = (List<RetryAttempt>)activityExecutionContext.TransientProperties[RetryAttempt.RetriesKey];
        records.Add(record);
    }
}