using Elsa.Common.Multitenancy;
using Elsa.Scheduling.Quartz.Contracts;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Quartz;
using Quartz.Simpl;
using Quartz.Spi;
using QuartzScheduler = Quartz.IScheduler;

namespace Elsa.Scheduling.Quartz.Services;

/// <summary>
/// Resolves Quartz jobs inside a DI scope that already has the trigger's tenant pushed,
/// so constructor dependencies bind to that tenant instead of the default tenant.
/// </summary>
[UsedImplicitly]
internal sealed class TenantAwareJobFactory(
    IServiceProvider serviceProvider,
    IOptions<QuartzOptions> options,
    ILogger<TenantAwareJobFactory> logger)
    : MicrosoftDependencyInjectionJobFactory(serviceProvider, options)
{
    private const string TenantIdKey = "TenantId";

    protected override IJob InstantiateJob(TriggerFiredBundle bundle, QuartzScheduler scheduler)
    {
        var tenantId = ReadTenantId(bundle);
        if (tenantId is null)
            return base.InstantiateJob(bundle, scheduler);

        Tenant? tenant;
        try
        {
            tenant = serviceProvider.GetRequiredService<ITenantFinder>().FindByIdAsync(tenantId).GetAwaiter().GetResult();
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Could not find tenant {TenantId} while building Quartz job {JobKey}. The failure will be retried on the next fire.", tenantId, bundle.JobDetail.Key);
            return new TenantLookupFailedJob(exception, serviceProvider, logger);
        }

        if (tenant is null)
        {
            logger.LogError("Could not find tenant {TenantId} while building Quartz job {JobKey}. Quartz will put this job's triggers in the ERROR state.", tenantId, bundle.JobDetail.Key);
            throw new InvalidOperationException($"Could not find tenant '{tenantId}' while building Quartz job '{bundle.JobDetail.Key}'. Quartz will put this job's triggers in the ERROR state.");
        }

        var accessor = serviceProvider.GetRequiredService<ITenantAccessor>();

        using (accessor.PushContext(tenant))
            return new TenantJob(base.InstantiateJob(bundle, scheduler), accessor, tenant);
    }

    private static string? ReadTenantId(TriggerFiredBundle bundle)
    {
        if (TryGetTenantId(bundle.Trigger.JobDataMap, out var tenantId))
            return tenantId;

        if (TryGetTenantId(bundle.JobDetail.JobDataMap, out tenantId))
            return tenantId;

        return null;
    }

    private static bool TryGetTenantId(JobDataMap map, out string tenantId)
    {
        tenantId = "";

        if (!map.TryGetString(TenantIdKey, out var value) || string.IsNullOrWhiteSpace(value))
            return false;

        tenantId = value;
        return true;
    }

    /// <summary>
    /// Not <see cref="IDisposable"/> on purpose: Quartz unwraps <see cref="IJobWrapper.Target"/> and async-disposes the base scope.
    /// </summary>
    private sealed class TenantJob(IJob inner, ITenantAccessor accessor, Tenant tenant) : IJob, IJobWrapper
    {
        public IJob Target => inner;

        public async Task Execute(IJobExecutionContext context)
        {
            using (accessor.PushContext(tenant))
                await inner.Execute(context);
        }
    }

    private sealed class TenantLookupFailedJob(Exception exception, IServiceProvider serviceProvider, ILogger logger) : IJob
    {
        public async Task Execute(IJobExecutionContext context)
        {
            var retryScheduler = serviceProvider.GetService<IQuartzJobRetryScheduler>();
            if (retryScheduler is not null)
            {
                if (retryScheduler.IsRetryable(exception) && await retryScheduler.ScheduleRetryAsync(context, exception, context.CancellationToken))
                    return;

                logger.LogError(exception, "No retry was scheduled for job {JobKey} after a tenant lookup failure.", context.JobDetail.Key);
                return;
            }

            throw exception;
        }
    }
}
