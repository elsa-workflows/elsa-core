using Microsoft.EntityFrameworkCore;

namespace Elsa.Persistence.EntityFramework.Core.Services
{
    public class ElsaContextFactory<TElsaContext> : IElsaContextFactory where TElsaContext : ElsaContext
    {
        private readonly IDbContextFactory<TElsaContext> _contextFactory;
        public ElsaContextFactory(IDbContextFactory<TElsaContext> contextFactory) => _contextFactory = contextFactory;
        public ElsaContext CreateDbContext() => _contextFactory.CreateDbContext();
    }
}