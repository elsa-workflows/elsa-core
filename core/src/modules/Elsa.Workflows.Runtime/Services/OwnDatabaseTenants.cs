using Elsa.Common;
using Elsa.Common.Multitenancy;
using Microsoft.Extensions.Logging;

namespace Elsa.Workflows.Runtime.Services;

/// <summary>
/// Finds tenants whose data lives in their own database, which tenant-agnostic queries against the shared store cannot see.
/// </summary>
internal static class OwnDatabaseTenants
{
    // Runtime does not reference Elsa.Persistence.EFCore.Common, which owns this section name.
    private const string ConnectionStringsSectionName = "ConnectionStrings";

    public static bool HasOwnDatabase(this Tenant tenant) =>
        tenant.Configuration.GetSection(ConnectionStringsSectionName).GetChildren().Any(x => !string.IsNullOrWhiteSpace(x.Value));

    public static async Task<List<Tenant>> TryListAsync(ITenantService? tenantService, ITenantScopeFactory? tenantScopeFactory, ILogger logger, CancellationToken cancellationToken)
    {
        if (tenantService is null || tenantScopeFactory is null)
            return [];

        try
        {
            var tenants = await tenantService.ListAsync(cancellationToken);
            return tenants.Where(x => x.HasOwnDatabase()).ToList();
        }
        catch (Exception ex) when (IsRecoverable(ex, cancellationToken))
        {
            logger.LogWarning(ex, "Failed to list tenants; tenants with their own database are not handled separately in this run.");
            return [];
        }
    }

    // RunOnceForAllTenants may run under an own-database tenant, whose scoped stores would not see the shared database.
    // Tenant.Default carries no configuration, so its scope resolves the shared database.
    public static bool NeedsSharedScope(IReadOnlyCollection<Tenant> ownDatabaseTenants, ITenantAccessor? tenantAccessor) =>
        ownDatabaseTenants.Count > 0 && tenantAccessor?.Tenant?.HasOwnDatabase() == true;

    public static bool IsRecoverable(Exception ex, CancellationToken cancellationToken) =>
        !ex.IsFatal() && !(ex is OperationCanceledException && cancellationToken.IsCancellationRequested);
}
