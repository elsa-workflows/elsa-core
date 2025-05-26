using Elsa.Common.Models;
using Elsa.Extensions;
using Elsa.Resilience.Entities;
using Elsa.Workflows.Runtime;

namespace Elsa.Resilience.Recorders;

public class ActivityExecutionContextRetryAttemptReader(IActivityExecutionStore activityExecutionStore) : IRetryAttemptReader
{
    public async Task<Page<RetryAttemptRecord>> ReadAttemptsAsync(string activityInstanceId, PageArgs? pageArgs = null, CancellationToken cancellationToken = default)
    {
        var record = await activityExecutionStore.FindAsync(new()
        {
            Id = activityInstanceId
        }, cancellationToken);

        if (record?.Properties == null || !record.Properties.TryGetValue("RetryAttempts", out var value) || value is not ICollection<RetryAttemptRecord> retryAttempts)
            return Page.Empty<RetryAttemptRecord>();

        return retryAttempts.Paginate(pageArgs);
    }
}