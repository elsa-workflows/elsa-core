using Elsa.Resilience;
using Polly;
using Polly.Retry;

namespace Elsa.Http.Resilience;

[ResilienceCategory("HTTP")]
public class HttpResilienceStrategy : IResilienceStrategy
{
    public string Id { get; set; } = null!;
    public string DisplayName { get; set; } = null!;
    public int RetryCount { get; set; } = 3;
    public double BackoffFactor { get; set; } = 2.0;

    public async Task<T> ExecuteAsync<T>(Func<Task<T>> action)
    {
        AsyncRetryPolicy policy = Policy
            .Handle<Exception>()
            .WaitAndRetryAsync(
                RetryCount,
                retryAttempt => TimeSpan.FromSeconds(Math.Pow(BackoffFactor, retryAttempt))
            );

        return await policy.ExecuteAsync(action);
    }
}