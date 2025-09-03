using System.Text.Json;
using Elsa.Expressions.Helpers;
using Elsa.Extensions;
using Elsa.Resilience.Entities;
using Elsa.Resilience.Extensions;
using Elsa.Resilience.Models;
using Elsa.Resilience.Serialization;
using Elsa.Workflows;
using Elsa.Workflows.State;
using Polly;
using Polly.Telemetry;

namespace Elsa.Resilience;

public class ResilientActivityInvoker(
    IResilienceStrategyConfigEvaluator resilienceStrategyConfigEvaluator,
    IRetryAttemptRecorder retryAttemptRecorder,
    IIdentityGenerator identityGenerator,
    ResilienceStrategySerializer resilienceStrategySerializer) : IResilientActivityInvoker
{
    private const string ResilienceStrategyIdPropKey = "resilienceStrategy";
    private const string RetryAttemptsCountKey = "RetryAttemptsCount";

    public async Task<T> InvokeAsync<T>(IResilientActivity activity, ActivityExecutionContext context, Func<Task<T>> action, CancellationToken cancellationToken = default)
    {
        // Get the resilience strategy.
        var strategyConfig = GetStrategyConfig(activity);
        var resilienceStrategy = await resilienceStrategyConfigEvaluator.EvaluateAsync(strategyConfig, context.ExpressionExecutionContext, cancellationToken);

        // If no resilience strategy is configured, execute the action as-is.
        if (resilienceStrategy == null)
            return await action();

        // Record the applied strategy as part of the activity execution context for diagnostics.
        var resilienceStrategyModel = JsonSerializer.SerializeToNode(resilienceStrategy, resilienceStrategySerializer.SerializerOptions)!;
        context.SetResilienceStrategy(resilienceStrategyModel);

        // Create a resilience pipeline builder.
        var builder = CreateResiliencePipelineBuilder<T>();
        var retries = new List<RetryAttempt>();
        context.TransientProperties[RetryAttempt.RetriesKey] = retries;

        // Create a resilience context.
        var resilienceContext = ResilienceContextPool.Shared.Get(cancellationToken);
        resilienceContext.Properties.Set(new(nameof(ActivityExecutionContext)), context);

        try
        {
            // Configure the resilience pipeline.
            await resilienceStrategy.ConfigurePipeline(builder, resilienceContext);
            var pipeline = builder.Build();

            // Execute the action within the resilience pipeline.
            var result = await pipeline.ExecuteAsync<T>(async _ => await action(), resilienceContext);

            // Record the retry attempts.
            await RecordRetryAttempts(activity, context, retries, cancellationToken);

            return result;
        }
        finally
        {
            ResilienceContextPool.Shared.Return(resilienceContext);
        }

    }

    private async Task RecordRetryAttempts(IResilientActivity activity, ActivityExecutionContext context, ICollection<RetryAttempt> attempts, CancellationToken cancellationToken = default)
    {
        if (attempts.Count > 0)
        {
            var records = Map(context, activity, attempts);
            var recordContext = new RecordRetryAttemptsContext(context, records, cancellationToken);
            await retryAttemptRecorder.RecordAsync(recordContext);

            // Propagate a flag that retries have occurred. This information can then be used to show the retry attempts in the workflow designer.
            context.SetRetriesAttemptedFlag();

            context.SetExtensionsMetadata(new Dictionary<string, object?>(){{ RetryAttemptsCountKey, attempts.Count }});
        }
    }

    private ResiliencePipelineBuilder<T> CreateResiliencePipelineBuilder<T>()
    {
        var telemetryOptions = new TelemetryOptions();
        telemetryOptions.TelemetryListeners.Add(new RetryTelemetryListener());
        return new ResiliencePipelineBuilder<T>().ConfigureTelemetry(telemetryOptions);
    }

    private ResilienceStrategyConfig? GetStrategyConfig(IResilientActivity resilientActivity)
    {
        return !resilientActivity.CustomProperties.TryGetValue(ResilienceStrategyIdPropKey, out var value)
            ? null
            : value.ConvertTo<ResilienceStrategyConfig>();
    }

    private ICollection<RetryAttemptRecord> Map(ActivityExecutionContext activityExecutionContext, IResilientActivity resilientActivity, ICollection<RetryAttempt> attempts)
    {
        return attempts.Select(x => Map(activityExecutionContext, resilientActivity, x)).ToList();
    }

    private RetryAttemptRecord Map(ActivityExecutionContext activityExecutionContext, IResilientActivity resilientActivity, RetryAttempt attempt)
    {
        var details = resilientActivity.CollectRetryDetails(activityExecutionContext, attempt).Where(x => x.Value != null).ToDictionary(x => x.Key, x => x.Value!);
        return new()
        {
            Id = identityGenerator.GenerateId(),
            ActivityInstanceId = activityExecutionContext.Id,
            ActivityId = activityExecutionContext.Activity.Id,
            WorkflowInstanceId = activityExecutionContext.WorkflowExecutionContext.Id,
            AttemptNumber = attempt.AttemptNumber,
            RetryDelay = attempt.RetryDelay,
            Details = details
        };
    }
}