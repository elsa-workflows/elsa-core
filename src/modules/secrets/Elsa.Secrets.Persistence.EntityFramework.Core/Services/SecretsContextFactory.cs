using Microsoft.EntityFrameworkCore;

namespace Elsa.Secrets.Persistence.EntityFramework.Core.Services
{
    public class SecretsContextFactory<TSecretsContext> : ISecretsContextFactory where TSecretsContext : SecretsContext
    {
        private readonly IDbContextFactory<TSecretsContext> _contextFactory;
        public SecretsContextFactory(IDbContextFactory<TSecretsContext> contextFactory) => _contextFactory = contextFactory;
        public SecretsContext CreateDbContext() => _contextFactory.CreateDbContext();
    }
}
