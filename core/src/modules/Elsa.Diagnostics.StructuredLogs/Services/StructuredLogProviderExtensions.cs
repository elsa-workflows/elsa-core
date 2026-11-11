using Elsa.Diagnostics.StructuredLogs.Contracts;
using Elsa.Diagnostics.StructuredLogs.Models;

namespace Elsa.Diagnostics.StructuredLogs.Services;

public static class StructuredLogProviderExtensions
{
    /// <summary>
    /// Gets recent events visible to the given tenant. A null tenant means no tenancy, so nothing is clamped.
    /// The provider applies the tenant filter before its take limit; the default tenant cannot be expressed as a filter, so its result is clamped afterwards and may hold fewer items than requested.
    /// </summary>
    public static async ValueTask<RecentStructuredLogsResult> GetRecentForTenantAsync(this IStructuredLogProvider provider, StructuredLogFilter filter, string? tenantId, CancellationToken cancellationToken = default)
    {
        if (tenantId == null)
            return await provider.GetRecentAsync(filter, cancellationToken);

        var scopedFilter = tenantId.Length > 0 ? filter with { TenantId = tenantId } : filter;
        var result = await provider.GetRecentAsync(scopedFilter, cancellationToken);
        return result with { Items = result.Items.Where(x => StructuredLogFilterEvaluator.IsVisibleToTenant(x, tenantId)).ToList() };
    }
}
