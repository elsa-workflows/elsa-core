using Elsa.Common.Multitenancy;
using Elsa.Scheduling.Quartz.Contracts;
using Quartz;

namespace Elsa.Scheduling.Quartz.Services;

public class JobKeyProvider(ITenantAccessor tenantAccessor) : IJobKeyProvider
{
    public JobKey GetJobKey<TJob>() where TJob : IJob
    {
        return new(typeof(TJob).Name, GetGroupName());
    }

    public string GetGroupName()
    {
        var tenantId = tenantAccessor.Tenant?.Id;
        return string.IsNullOrWhiteSpace(tenantId) ? "Default" : tenantId;
    }
}