using Elsa.Common.Models;
using Elsa.Resilience.Entities;

namespace Elsa.Resilience;

public interface IRetryAttemptReader
{
    Task<Page<RetryAttemptRecord>> ReadAttemptsAsync(string activityInstanceId, PageArgs? pageArgs = null, CancellationToken cancellationToken = default);   
}