using Elsa.Attributes;
using Elsa.Persistence.EntityFramework.Core;
using Microsoft.EntityFrameworkCore;

namespace Elsa.Persistence.EntityFramework.SqlServer
{
    [Feature("DefaultPersistence:EntityFrameworkCore:SqlServer")]
    public class Startup : EntityFrameworkCoreStartupBase
    {
        protected override string ProviderName => "SqlServer";
        protected override void Configure(DbContextOptionsBuilder options, string connectionString) => options.UseSqlServer(connectionString);
    }
}