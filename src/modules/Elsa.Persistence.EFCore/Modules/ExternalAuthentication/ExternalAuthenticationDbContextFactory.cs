using Elsa.Persistence.EFCore.Modules.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Persistence.EFCore.Modules.ExternalAuthentication;

/// <summary>Creates identity contexts from a short-lived scope so singleton atomic stores never capture tenant-scoped services.</summary>
public sealed class ExternalAuthenticationDbContextFactory(IServiceScopeFactory scopeFactory)
{
    public async ValueTask<Lease> CreateAsync(CancellationToken cancellationToken = default)
    {
        var scope = scopeFactory.CreateAsyncScope();
        try
        {
            var factory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<IdentityElsaDbContext>>();
            var dbContext = await factory.CreateDbContextAsync(cancellationToken);
            return new Lease(scope, dbContext);
        }
        catch
        {
            await scope.DisposeAsync();
            throw;
        }
    }

    public sealed class Lease(AsyncServiceScope scope, IdentityElsaDbContext dbContext) : IAsyncDisposable
    {
        public IdentityElsaDbContext DbContext { get; } = dbContext;

        public async ValueTask DisposeAsync()
        {
            await DbContext.DisposeAsync();
            await scope.DisposeAsync();
        }
    }
}
