using Elsa.Attributes;
using Elsa.Persistence.EntityFramework.Core;
using Elsa.Persistence.EntityFramework.Core.Options;
using Microsoft.EntityFrameworkCore;

namespace Elsa.Persistence.EntityFramework.SqlServer
{
    [Feature("DefaultPersistence:EntityFrameworkCore:SqlServer")]
    public class Startup : EntityFrameworkCoreStartupBase
    {
        protected override string ProviderName => "SqlServer";
        protected override void Configure(DbContextOptionsBuilder options, ElsaDbOptions elsaDbOptions)
            => options.UseSqlServer(elsaDbOptions.ConnectionString);
    }
}