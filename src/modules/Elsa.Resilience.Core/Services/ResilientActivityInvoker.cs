using Elsa.Expressions.Helpers;
using Elsa.Resilience.Diagnostics;
using Elsa.Resilience.Models;
using Elsa.Workflows;
using Polly;
using Polly.Telemetry;

namespace Elsa.Resilience;

public class ResilientActivityInvoker(IResilienceStrategyConfigEvaluator configEvaluator, IRetryAttemptRecorder retryAttemptRecorder) : IResilientActivityInvoker
{
    private const string ResilienceStrategyIdPropKey = "resilienceStrategy";

    public async Task<T> InvokeAsync<T>(IResilientActivity activity, ActivityExecutionContext context, Func<Task<T>> action, CancellationToken cancellationToken = default)
    {
        var strategyConfig = GetStrategyConfig(activity);
        var strategy = await configEvaluator.EvaluateAsync(strategyConfig, context.ExpressionExecutionContext, cancellationToken);
        
        if (strategy == null)
            return await action();
        
        var telemetryOptions = new TelemetryOptions();
        telemetryOptions.TelemetryListeners.Add(new RetryTelemetryListener());
        var builder = new ResiliencePipelineBuilder<T>().ConfigureTelemetry(telemetryOptions);
        var ctx = ResilienceContextPool.Shared.Get(cancellationToken);
        var retries = new List<RetryAttempt>();
        context.TransientProperties[RetryAttempt.RetriesKey] = retries;
        
        try
        {
            
            ctx.Properties.Set(new(nameof(ActivityExecutionContext)), context);
            await strategy.ConfigurePipeline(builder, ctx);
            var pipeline = builder.Build();
            var result = await pipeline.ExecuteAsync<T>(async c => await action(), ctx);
            
            if (retries.Count > 0)
            {
                var recordContext = new RecordRetryAttemptsContext(context, retries, cancellationToken);
                await retryAttemptRecorder.RecordAsync(recordContext);
            }
            
            return result;
        }
        finally
        {
            ResilienceContextPool.Shared.Return(ctx);
        }
        
    }

    private ResilienceStrategyConfig? GetStrategyConfig(IResilientActivity resilientActivity)
    {
        return !resilientActivity.CustomProperties.TryGetValue(ResilienceStrategyIdPropKey, out var value)
            ? null
            : value.ConvertTo<ResilienceStrategyConfig>();
    }
}