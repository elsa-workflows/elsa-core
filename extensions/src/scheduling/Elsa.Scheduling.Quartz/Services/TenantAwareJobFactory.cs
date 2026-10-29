using Elsa.Common.Multitenancy;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Quartz;
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
    ILogger<TenantAwareJobFactory> logger) : IJobFactory
{
    private const string TenantIdKey = "TenantId";

    public IJob NewJob(TriggerFiredBundle bundle, QuartzScheduler scheduler)
    {
        var tenantId = ReadTenantId(bundle);
        var scope = serviceProvider.CreateScope();
        IDisposable? tenantContext = null;

        try
        {
            if (tenantId is not null)
            {
                var tenantFinder = serviceProvider.GetRequiredService<ITenantFinder>();
                var tenantAccessor = serviceProvider.GetRequiredService<ITenantAccessor>();
                var tenant = FindTenantOrThrow(tenantFinder, bundle, tenantId);
                tenantContext = tenantAccessor.PushContext(tenant);
            }

            var job = (IJob)ActivatorUtilities.GetServiceOrCreateInstance(scope.ServiceProvider, bundle.JobDetail.JobType);
            return new ScopedJob(job, scope, tenantContext);
        }
        catch
        {
            tenantContext?.Dispose();
            scope.Dispose();
            throw;
        }
    }

    public void ReturnJob(IJob job) => (job as IDisposable)?.Dispose();

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

    private Tenant FindTenantOrThrow(ITenantFinder tenantFinder, TriggerFiredBundle bundle, string tenantId)
    {
        Tenant? tenant;

        try
        {
            tenant = tenantFinder.FindByIdAsync(tenantId).GetAwaiter().GetResult();
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Could not find tenant {TenantId} while building Quartz job {JobKey}.", tenantId, bundle.JobDetail.Key);
            throw;
        }

        if (tenant is not null)
            return tenant;

        logger.LogError("Could not find tenant {TenantId} while building Quartz job {JobKey}.", tenantId, bundle.JobDetail.Key);
        throw new InvalidOperationException($"Could not find tenant '{tenantId}' while building Quartz job '{bundle.JobDetail.Key}'.");
    }

    private sealed class ScopedJob(IJob innerJob, IServiceScope scope, IDisposable? tenantContext) : IJob, IJobWrapper, IDisposable
    {
        public IJob Target => innerJob;

        public Task Execute(IJobExecutionContext context) => innerJob.Execute(context);

        public void Dispose()
        {
            tenantContext?.Dispose();
            (innerJob as IDisposable)?.Dispose();
            scope.Dispose();
        }
    }
}
