using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Elsa.Secrets.Persistence.EntityFramework.Core.Services
{
    public class SecretsContextFactory<TSecretsContext> : ISecretsContextFactory where TSecretsContext : SecretsContext
    {
        private readonly IDbContextFactory<TSecretsContext> _contextFactory;
        public SecretsContextFactory(IDbContextFactory<TSecretsContext> contextFactory) => _contextFactory = contextFactory;
        public SecretsContext CreateDbContext() => _contextFactory.CreateDbContext();
    }
}
