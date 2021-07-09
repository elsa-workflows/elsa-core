using Elsa.Attributes;
using Elsa.Persistence.EntityFramework.Core;
using Microsoft.EntityFrameworkCore;

namespace Elsa.Persistence.EntityFramework.PostgreSql
{
    [Feature("DefaultPersistence:EntityFrameworkCore:PostgreSql")]
    public class Startup : EntityFrameworkCoreStartupBase
    {
        protected override string ProviderName => "PostgreSql";
        protected override void Configure(DbContextOptionsBuilder options, string connectionString) => options.UsePostgreSql(connectionString);
    }
}