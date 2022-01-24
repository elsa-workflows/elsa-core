using Elsa.Activities.SQL.Persistence;
using Elsa.Persistence.EntityFramework.Core.Services;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Elsa.Activities.SQL.Services
{
    public class SqlActivityContextFactory<TSqlActivityContext> : ISqlActivityFactory where TSqlActivityContext : SqlActivityContext
    {
        private readonly IDbContextFactory<TSqlActivityContext> _contextFactory;
        public SqlActivityContextFactory(IDbContextFactory<TSqlActivityContext> contextFactory) => _contextFactory = contextFactory;
        public SqlActivityContext CreateDbContext() => _contextFactory.CreateDbContext();
    }

    public interface ISqlActivityFactory : IContextFactory<SqlActivityContext>
    {
    }
}
