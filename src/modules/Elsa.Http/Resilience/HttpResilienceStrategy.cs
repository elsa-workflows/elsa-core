using Elsa.Http.Extensions;
using Elsa.Resilience;
using Polly;
using Polly.Retry;

namespace Elsa.Http.Resilience;

[ResilienceCategory("HTTP")]
public class HttpResilienceStrategy : IResilienceStrategy
{
    public string Id { get; set; } = null!;
    public string DisplayName { get; set; } = null!;
    public int MaxRetryAttempts { get; set; } = 3;
    public bool UseJitter { get; set; }
    public TimeSpan Delay { get; set; } = TimeSpan.FromSeconds(1);
    public DelayBackoffType BackoffType { get; set; } = DelayBackoffType.Exponential;

    public Task ConfigurePipeline<T>(ResiliencePipelineBuilder<T> pipelineBuilder, ResilienceContext context)
    {
        if (typeof(T) != typeof(HttpResponseMessage))
            throw new NotSupportedException($"{nameof(HttpResilienceStrategy)} only supports HttpResponseMessage.");
        
        var options = new RetryStrategyOptions<T>
        {
            ShouldHandle = new PredicateBuilder<T>()
                .Handle<TimeoutException>()
                .Handle<HttpRequestException>()
                .HandleResult(response => ((HttpResponseMessage)(object)response!).StatusCode.IsTransientStatusCode()),
            MaxRetryAttempts = MaxRetryAttempts,
            Delay = Delay,
            UseJitter = UseJitter,
            BackoffType = BackoffType,
            Name = DisplayName
        };
        
        pipelineBuilder.AddRetry(options);
        return Task.CompletedTask;
    }
}