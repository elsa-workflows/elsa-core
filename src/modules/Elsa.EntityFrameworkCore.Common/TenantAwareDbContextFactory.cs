using Elsa.Common.Multitenancy;
using Elsa.Extensions;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;

namespace Elsa.EntityFrameworkCore;

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
        if (context is ElsaDbContextBase elsaContext) 
            elsaContext.TenantId = tenantAccessor.Tenant?.Id.NullIfEmpty();
    }
}