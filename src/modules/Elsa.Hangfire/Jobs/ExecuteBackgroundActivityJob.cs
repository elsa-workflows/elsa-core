using Elsa.Common.Multitenancy;
using Elsa.Workflows.Runtime;
using JetBrains.Annotations;

namespace Elsa.Hangfire.Jobs;

/// <summary>
/// A job that executes a background activity.
/// </summary>
[UsedImplicitly]
public class ExecuteBackgroundActivityJob(IBackgroundActivityInvoker backgroundActivityInvoker, ITenantFinder tenantFinder, ITenantAccessor tenantAccessor)
{
    /// <summary>
    /// Executes the job.
    /// </summary>
    public async Task ExecuteAsync(ScheduledBackgroundActivity scheduledBackgroundActivity, string? tenantId, CancellationToken cancellationToken = default)
    {
        var tenant = tenantId != null ? await tenantFinder.FindByIdAsync(tenantId, cancellationToken) : null;
        using var scope = tenantAccessor.PushContext(tenant);
        await backgroundActivityInvoker.ExecuteAsync(scheduledBackgroundActivity, cancellationToken);
    }
}