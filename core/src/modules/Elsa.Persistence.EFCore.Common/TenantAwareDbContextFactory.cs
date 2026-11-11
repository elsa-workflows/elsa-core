using Elsa.Common.Multitenancy;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;

namespace Elsa.Persistence.EFCore;

/// <summary>
/// A factory class that wraps and extends the functionality of an existing <see cref="IDbContextFactory{TDbContext}"/>
/// to provide multi-tenancy support. This ensures that the created DbContext instances are aware of the current tenant context.
/// </summary>
[UsedImplicitly]
public class TenantAwareDbContextFactory<TDbContext>(
    IDbContextFactory<TDbContext> decoratedFactory,
    ITenantAccessor tenantAccessor) : IDbContextFactory<TDbContext>
    where TDbContext : DbContext
{
    public TDbContext CreateDbContext()
    {
        var context = decoratedFactory.CreateDbContext();
        SetTenantId(context);
        return context;
    }

    public async Task<TDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default)
    {
        var context = await decoratedFactory.CreateDbContextAsync(cancellationToken);
        SetTenantId(context);
        return context;
    }

    private void SetTenantId(TDbContext context)
    {
        // Propagate the raw tenant id: a missing tenant context (Tenant == null) must stay null on the
        // context so ApplyTenantId can tell "no context" (keep explicit ids) apart from the default tenant ("").
        if (context is ElsaDbContextBase elsaContext)
            elsaContext.TenantId = tenantAccessor.Tenant?.Id;
    }
}