using Elsa.Common.Multitenancy;
using Elsa.Quartz.Contracts;
using Quartz;

namespace Elsa.Quartz.Services;

internal class JobKeyProvider(ITenantAccessor tenantAccessor) : IJobKeyProvider
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