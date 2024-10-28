using Elsa.Common.Multitenancy;
using Elsa.Tenants;
using Microsoft.Extensions.DependencyInjection;
using Quartz;

namespace Elsa.Quartz;

internal static class JobExecutionExtensions
{
    public static async Task<Tenant?> GetTenantAsync(this IJobExecutionContext context, IServiceScopeFactory scopeFactory)
    {
        if(!context.MergedJobDataMap.TryGetString("TenantId", out var tenantId))
            return null;
        
        if (string.IsNullOrWhiteSpace(tenantId))
            return null;
        
        using var scope = scopeFactory.CreateScope();
        var tenantsProvider = scope.ServiceProvider.GetRequiredService<ITenantsProvider>();
        var cancellationToken = context.CancellationToken;
        return await tenantsProvider.FindByIdAsync(tenantId, cancellationToken: cancellationToken);
    }
}