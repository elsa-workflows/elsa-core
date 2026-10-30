using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.ExternalAuthentication.Persistence.EFCore;

/// <summary>Creates external authentication contexts from a short-lived scope so singleton stores never capture tenant-scoped services.</summary>
public sealed class ExternalAuthenticationDbContextLeaseFactory(IServiceScopeFactory scopeFactory)
{
    public async ValueTask<Lease> CreateAsync(CancellationToken cancellationToken = default)
    {
        var scope = scopeFactory.CreateAsyncScope();
        try
        {
            var factory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<ExternalAuthenticationElsaDbContext>>();
            var dbContext = await factory.CreateDbContextAsync(cancellationToken);
            return new Lease(scope, dbContext);
        }
        catch
        {
            await scope.DisposeAsync();
            throw;
        }
    }

    public sealed class Lease(AsyncServiceScope scope, ExternalAuthenticationElsaDbContext dbContext) : IAsyncDisposable
    {
        public ExternalAuthenticationElsaDbContext DbContext { get; } = dbContext;

        public async ValueTask DisposeAsync()
        {
            await DbContext.DisposeAsync();
            await scope.DisposeAsync();
        }
    }
}
