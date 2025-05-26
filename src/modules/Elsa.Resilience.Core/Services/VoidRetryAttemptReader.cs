using Elsa.Common.Models;
using Elsa.Resilience.Entities;

namespace Elsa.Resilience;

public class VoidRetryAttemptReader : IRetryAttemptReader
{
    public static VoidRetryAttemptReader Instance { get; } = new();
    
    public Task<Page<RetryAttemptRecord>> ReadAttemptsAsync(string activityInstanceId, PageArgs? pageArgs = null, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(Page.Empty<RetryAttemptRecord>());
    }
}