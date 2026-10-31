using System.Runtime.ExceptionServices;
using Elsa.Common.Multitenancy;
using Elsa.Scheduling.Quartz;
using Elsa.Scheduling.Quartz.Contracts;
using Elsa.Scheduling.Quartz.Jobs;
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
internal sealed class TenantAwareJobFactory : MicrosoftDependencyInjectionJobFactory
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<TenantAwareJobFactory> _logger;

    public TenantAwareJobFactory(
        IServiceProvider serviceProvider,
        IOptions<QuartzOptions> options,
        ILogger<TenantAwareJobFactory> logger)
        : base(serviceProvider, options)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override IJob InstantiateJob(TriggerFiredBundle bundle, QuartzScheduler scheduler)
    {
        var tenantId = JobExecutionExtensions.ReadScheduledTenantId(bundle.Trigger.JobDataMap, bundle.JobDetail.JobDataMap);
        if (tenantId is null)
            return base.InstantiateJob(bundle, scheduler);

        Tenant? tenant;
        try
        {
            tenant = _serviceProvider.GetRequiredService<ITenantFinder>().FindByIdAsync(tenantId).GetAwaiter().GetResult();
        }
        catch (Exception exception)
        {
            _logger.LogWarning(exception, "Could not find tenant {TenantId} while building Quartz job {JobKey}.", tenantId, bundle.JobDetail.Key);
            return new TenantLookupFailedJob(exception, _serviceProvider, _logger, bundle.JobDetail.JobType);
        }

        if (tenant is null)
        {
            _logger.LogError("Could not find tenant {TenantId} while building Quartz job {JobKey}. Quartz will put this job's triggers in the ERROR state.", tenantId, bundle.JobDetail.Key);
            throw new InvalidOperationException($"Could not find tenant '{tenantId}' while building Quartz job '{bundle.JobDetail.Key}'. Quartz will put this job's triggers in the ERROR state.");
        }

        var accessor = _serviceProvider.GetRequiredService<ITenantAccessor>();

        using (accessor.PushContext(tenant))
            return new TenantJob(base.InstantiateJob(bundle, scheduler), accessor, tenant);
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

    private sealed class TenantLookupFailedJob(Exception exception, IServiceProvider serviceProvider, ILogger logger, Type jobType) : IJob
    {
        public async Task Execute(IJobExecutionContext context)
        {
            if (jobType == typeof(ExecuteBackgroundActivityJob))
            {
                logger.LogError(exception, "An error occurred while executing a background activity job {JobKey} after a tenant lookup failure.", context.JobDetail.Key);
                await context.DeleteJob(context.JobDetail.Key, context.CancellationToken);
                ExceptionDispatchInfo.Capture(exception).Throw();
            }

            var retryScheduler = serviceProvider.GetService<IQuartzJobRetryScheduler>();
            if (retryScheduler is not null)
            {
                if (retryScheduler.IsRetryable(exception) && await retryScheduler.ScheduleRetryAsync(context, exception, context.CancellationToken))
                    return;

                logger.LogError(exception, "No retry was scheduled for job {JobKey} after a tenant lookup failure.", context.JobDetail.Key);
                return;
            }

            ExceptionDispatchInfo.Capture(exception).Throw();
        }
    }
}
